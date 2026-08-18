using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace DefaultNamespace
{
    public struct GemTag : IComponentData
    {
    }

    public class GemAuthoring : MonoBehaviour
    {
        public class Baker : Baker<GemAuthoring>
        {
            public override void Bake(GemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<GemTag>(entity);
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }

    public partial struct CollectionGemSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var newCollectGemJob = new CollectGemJob()
            {
                GemLookup = SystemAPI.GetComponentLookup<GemTag>(true),
                DestrotEntityLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>(),
                GemsCollectedCountLookup = SystemAPI.GetComponentLookup<GemsCollectedCount>(),
                UpdateGemUILookup = SystemAPI.GetComponentLookup<UpdateGemUIFlag>()
            };

            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = newCollectGemJob.Schedule(simulationSingleton, state.Dependency);
        }
    }

    [BurstCompile]
    public struct CollectGemJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<GemTag> GemLookup;
        public ComponentLookup<DestroyEntityFlag> DestrotEntityLookup;
        public ComponentLookup<GemsCollectedCount> GemsCollectedCountLookup;
        public ComponentLookup<UpdateGemUIFlag> UpdateGemUILookup;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity gemEntity;
            Entity playerEntity;

            if (GemLookup.HasComponent(triggerEvent.EntityA) &&
                GemsCollectedCountLookup.HasComponent(triggerEvent.EntityB))
            {
                gemEntity = triggerEvent.EntityA;
                playerEntity = triggerEvent.EntityB;
            }
            else if (GemLookup.HasComponent(triggerEvent.EntityB) &&
                     GemsCollectedCountLookup.HasComponent(triggerEvent.EntityA))
            {
                gemEntity = triggerEvent.EntityB;
                playerEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            var gemsCollectedCount = GemsCollectedCountLookup[playerEntity];
            gemsCollectedCount.Value++;
            GemsCollectedCountLookup[playerEntity] = gemsCollectedCount;

            UpdateGemUILookup.SetComponentEnabled(playerEntity, true);
            DestrotEntityLookup.SetComponentEnabled(gemEntity, true);
        }
    }
}