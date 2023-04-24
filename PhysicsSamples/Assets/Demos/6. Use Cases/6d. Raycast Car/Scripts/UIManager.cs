using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {

    //From everything I could find online, there's no direct way to build UI using ECS.
    //This was the best approach I could find; UI is handled as a monobehaviour but reads component data from system.

    private PhysicsSimulationGroup physicsGroup;
    private World world;
    private EntityQuery activeVehicle;

    [SerializeField] private Text text;
    [SerializeField] private Slider slider;

    void Start() {
        world = World.DefaultGameObjectInjectionWorld;
        physicsGroup = world.GetExistingSystemManaged<PhysicsSimulationGroup>();
        activeVehicle = world.EntityManager.CreateEntityQuery(typeof(ActiveVehicle), typeof(Vehicle));
    }

    void Update() {
        if (!activeVehicle.IsEmptyIgnoreFilter) {
            float value = 0f;
            var activeVehicleNativeArray = activeVehicle.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < activeVehicleNativeArray.Length; i++) {
                var fuelComp = physicsGroup.GetComponentLookup<VehicleFuelComponent>(true)[activeVehicleNativeArray[i]];
                value = fuelComp.FuelAmount;
            }

            text.text = "Remaining fuel: " + value;
            slider.value = value/100f;
        }
    }
}
