using Demos;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;

struct FuelComponent : IComponentData {
    public float FuelAmount;
    public float MaxFuel;
}

class VehicleFuel : MonoBehaviour {

    [Header("Fuel System")]
    public float MaxFuel;
    public float FuelAmount;

    class FuelBaker : Baker<VehicleFuel> {

        public override void Bake(VehicleFuel fuelComp) {
            fuelComp.FuelAmount = fuelComp.MaxFuel; //Fuel tank will always start full.
            Debug.Log("Baking vehicle fuel component");
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new FuelComponent {
                FuelAmount = fuelComp.FuelAmount,
                MaxFuel = fuelComp.MaxFuel
            });
        }
    }
}

[UpdateAfter(typeof(VehicleMechanicsSystem))]
[BurstCompile]
public partial class VehicleFuelSystem : SystemBase {

    [BurstCompile]
    protected override void OnUpdate() {
        Debug.Log("Vehicle fuel system updating!");

        Dependency = Entities
            .WithName("PrepareVehiclesJob")
            .WithBurst()
            .ForEach((
                Entity entity, ref FuelComponent vehicleFuel) => {
                vehicleFuel.FuelAmount -= 0.1f;
                //Debug.Log("Fuel: " + vehicleFuel.FuelAmount);


            })
            .Schedule(Dependency);

        Dependency.Complete();
    }
}
