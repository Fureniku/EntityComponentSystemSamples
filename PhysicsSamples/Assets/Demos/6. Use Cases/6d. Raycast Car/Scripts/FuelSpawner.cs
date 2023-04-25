using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct Spawner : IComponentData {
    public Entity Prefab;
    public float3 SpawnPos;
    public float NextSpawnTime;
    public float SpawnRate;
    public int CurrentAmount;
    public int MaxAmount;
}

public struct FuelCanister : IComponentData {
    public float FuelAmount;
}

[BurstCompile]
public partial struct SpawnerSystem : ISystem {

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        new ProcessSpawnerJob {
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb,
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

            Entity newEntity = Ecb.Instantiate(chunkIndex, spawner.Prefab);
            float2 randXY = random.NextFloat2(new float2(-175f, -175f), new float2(175f, 175f));

            Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(new float3(randXY.x, 10f, randXY.y)));

            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate;
            spawner.CurrentAmount++;
        }
    }
}

