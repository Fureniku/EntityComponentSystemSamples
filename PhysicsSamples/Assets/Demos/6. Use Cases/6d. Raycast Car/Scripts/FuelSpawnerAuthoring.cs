using Unity.Entities;
using UnityEngine;

public class FuelSpawnerAuthoring : MonoBehaviour {
    public GameObject Prefab;
    public float SpawnRate;
    public float FuelAmount;
    public int MaxAmount;
}

class FuelSpawnerBaker : Baker<FuelSpawnerAuthoring> {

    public override void Bake(FuelSpawnerAuthoring authoring) {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new Spawner {
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
            SpawnPos = authoring.transform.position,
            NextSpawnTime = 0.0f,
            CurrentAmount = 0,
            MaxAmount = authoring.MaxAmount,
            SpawnRate = authoring.SpawnRate
        });
    }
}
