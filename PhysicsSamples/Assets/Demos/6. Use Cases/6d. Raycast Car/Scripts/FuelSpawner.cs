using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

//Control spawning of fuel barrels
public struct Spawner : IComponentData {
    public Entity Prefab;
    public float3 SpawnPos;
    public float NextSpawnTime;
    public float SpawnRate;
    public int CurrentAmount;
    public int MaxAmount;
}

//The fuel barrel itself
public struct FuelCanister : IComponentData {
    public float FuelAmount;
}

//A tag to mark something as collectable. Could be reused for other pickups.
public struct Collectable : IComponentData { }

//System to manage spawning of fuel barrels
[BurstCompile]
public partial struct SpawnerSystem : ISystem {

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        new ProcessSpawnerJob {
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb,
            //Create a new random with a randomised seed for each job
            random = new Random((uint)UnityEngine.Random.Range(1, 100000))
        }.ScheduleParallel();
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

[BurstCompile]
public partial struct ProcessSpawnerJob : IJobEntity {
    public EntityCommandBuffer.ParallelWriter Ecb;
    public double ElapsedTime;
    public Random random;

    private void Execute([ChunkIndexInQuery] int chunkIndex, ref Spawner spawner) {
        if (spawner.NextSpawnTime < ElapsedTime && spawner.CurrentAmount < spawner.MaxAmount) {
            //Create entity and add the components to it
            Entity newEntity = Ecb.Instantiate(chunkIndex, spawner.Prefab);
            Ecb.AddComponent<FuelCanister>(chunkIndex, newEntity);
            Ecb.AddComponent<Collectable>(chunkIndex, newEntity);

            //Select a random location on the map to spawn it
            float2 randXY = random.NextFloat2(new float2(-175f, -175f), new float2(175f, 175f));

            //Spawn the barrel
            Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(new float3(randXY.x, 10f, randXY.y)));

            //Update spawner data
            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate;
            spawner.CurrentAmount++;
        }
    }
}

[BurstCompile]
public partial class ConsumeFuelBarrelSystem : SystemBase {

    [BurstCompile]
    //Handle consuming of fuel barrels
    protected override void OnUpdate() {
        EntityCommandBuffer.ParallelWriter ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged).AsParallelWriter();
        EntityQuery activeVehicleQuery = GetEntityQuery(typeof(ActiveVehicle), typeof(Vehicle));
        EntityQuery collectablesQuery = GetEntityQuery(typeof(Collectable));

        //We only ever have one active vehicle, so it'll always be at index 0.
        Entity vehicle = activeVehicleQuery.ToEntityArray(Allocator.TempJob)[0];
        NativeArray<Entity> nativeCollectables = collectablesQuery.ToEntityArray(Allocator.TempJob);

        //If we're going to destroy a collectable, set this to its index in the array.
        int destroyEntity = -1;

        for (int i = 0; i < nativeCollectables.Length; i++) {
            LocalToWorld ltw = EntityManager.GetComponentData<LocalToWorld>(nativeCollectables[i]);
            float3 collPos = ltw.Position;

            //If the barrel is close to the player, we'll destroy it.
            if (math.distance(SystemAPI.GetComponent<LocalToWorld>(vehicle).Position, collPos) < 5) {
                destroyEntity = i;
                break;
            }
        }

        //A barrel was close enough to be destroyed, so schedule the jobs.
        if (destroyEntity > 0) {
            new AddFuelJob().ScheduleParallel();
            new RemoveBarrelJob{Ecb = ecb, DestroyableEntity = nativeCollectables[destroyEntity]}.ScheduleParallel();
        }
    }
}

[BurstCompile]
partial struct AddFuelJob : IJobEntity {

    //Job to add fuel to the vehicle
    public void Execute(ActiveVehicle activeVehicle, ref VehicleFuelComponent fuelComponent) {
        fuelComponent.FuelAmount += 10f;

        if (fuelComponent.FuelAmount > fuelComponent.MaxFuel) {
            fuelComponent.FuelAmount = fuelComponent.MaxFuel;
        }
    }
}

[BurstCompile]
partial struct RemoveBarrelJob : IJobEntity {

    public EntityCommandBuffer.ParallelWriter Ecb;
    public Entity DestroyableEntity;

    //Job to destroy the barrel entity, and inform the spawner that there's one less barrel and a new one can be spawned.
    public void Execute([EntityIndexInQuery] int entityIndex, ref Spawner spawner) {
        spawner.CurrentAmount--;
        Ecb.DestroyEntity(entityIndex, DestroyableEntity);
    }
}
