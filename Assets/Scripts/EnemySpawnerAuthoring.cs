using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DefaultNamespace
{
    public struct EnemySpawnData : IComponentData
    {
        public Entity EnemyPrefab;
        public float SpawnInterval;
        public float SpawnDistance;
    }

    public struct EnemySpawnState : IComponentData
    {
        public float SpawnTimer;
        public Random Random;
    }

    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject EnemyPrefab;
        public float SpawnInterval;
        public float SpawnDistance;
        public uint RandomSeed;

        private class Baker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemySpawnData()
                {
                    EnemyPrefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic),
                    SpawnInterval = authoring.SpawnInterval,
                    SpawnDistance = authoring.SpawnDistance
                });
                AddComponent(entity, new EnemySpawnState()
                {
                    SpawnTimer = authoring.SpawnInterval,
                    Random = Random.CreateFromIndex(authoring.RandomSeed)
                });
            }
        }
    }

    public partial struct EnemySpawnSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (spawnState, spawnData) in SystemAPI.Query<RefRW<EnemySpawnState>, EnemySpawnData>())
            {
                spawnState.ValueRW.SpawnTimer -= deltaTime;
                if (spawnState.ValueRW.SpawnTimer > 0) continue;
                spawnState.ValueRW.SpawnTimer = spawnData.SpawnInterval;
            }
        }
    }
}