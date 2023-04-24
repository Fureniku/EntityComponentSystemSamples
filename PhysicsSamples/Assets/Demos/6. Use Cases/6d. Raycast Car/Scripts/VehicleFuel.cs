using Demos;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

struct VehicleFuelComponent : IComponentData {
    public float MaxFuel;
    public float FuelAmount;
    public float FuelDrainRate;
}

class VehicleFuel : MonoBehaviour {

    [Header("Fuel System")]
    public float MaxFuel;
    public float FuelAmount;
    public float FuelDrainRate;

    class FuelBaker : Baker<VehicleFuel> {

        public override void Bake(VehicleFuel fuelComp) {
            fuelComp.FuelAmount = fuelComp.MaxFuel; //Fuel tank will always start full.
            Debug.Log("Baking vehicle fuel component");
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new VehicleFuelComponent {
                MaxFuel = fuelComp.MaxFuel,
                FuelAmount = fuelComp.FuelAmount,
                FuelDrainRate = fuelComp.FuelDrainRate
            });
        }
    }
}

[UpdateAfter(typeof(VehicleMechanicsSystem))]
[BurstCompile]
public partial class VehicleFuelSystem : SystemBase {

    [BurstCompile]
    protected override void OnUpdate() {
        Dependency = Entities
            .WithName("PrepareVehiclesJob")
            .WithBurst()
            .ForEach((Entity entity, ref VehicleFuelComponent vehicleFuel, ref VehicleSpeed vehicleSpeed) => {
                if (vehicleSpeed.DriveEngaged == 1) {
                    if (vehicleFuel.FuelAmount > 0f) {
                        vehicleFuel.FuelAmount -= vehicleFuel.FuelDrainRate;
                    } else {
                        vehicleSpeed.TopSpeed = 0;
                        vehicleFuel.FuelAmount = 0;
                    }
                }
            })
            .Schedule(Dependency);

        Dependency.Complete();
    }
}
