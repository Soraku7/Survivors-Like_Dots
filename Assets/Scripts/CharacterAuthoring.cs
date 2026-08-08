using DefaultNamespace;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;

public struct InitializeCharacterFlag : IComponentData, IEnableableComponent
{
}

public struct CharacterMoveDirection : IComponentData
{
    public float2 Value;
}

public struct CharacterMoveSpeed : IComponentData
{
    public float Value;
}

[MaterialProperty("_FacingDirection")]
public struct FacingDirectionOverride : IComponentData
{
    public float Value;
}

public struct CharacterMaxHitPoints : IComponentData
{
    public int Value;
}

public struct CharacterCurrentHitPoints : IComponentData
{
    public int Value;
}

public class CharacterAuthoring : MonoBehaviour
{
    public float MoveSpeed;
    public int HitPoints;

    private class Baker : Baker<CharacterAuthoring>
    {
        public override void Bake(CharacterAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<InitializeCharacterFlag>(entity);
            AddComponent<CharacterMoveDirection>(entity);
            AddComponent(entity, new CharacterMoveSpeed()
            {
                Value = authoring.MoveSpeed
            });
            AddComponent(entity, new FacingDirectionOverride()
            {
                Value = 1
            });
            AddComponent(entity, new CharacterMaxHitPoints()
            {
                Value = authoring.HitPoints
            });
        }
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct CharacterInitializationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (mass, shouldInitialize) in SystemAPI
                     .Query<RefRW<PhysicsMass>, EnabledRefRW<InitializeCharacterFlag>>())
        {
            mass.ValueRW.InverseInertia = float3.zero;
            shouldInitialize.ValueRW = false;
        }
    }
}

public partial struct CharacterMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        foreach (var (velocity, facingDirection, direction, speed, entity) in SystemAPI
                     .Query<RefRW<PhysicsVelocity>, RefRW<FacingDirectionOverride>, CharacterMoveDirection,
                         CharacterMoveSpeed>().WithEntityAccess())
        {
            var moveSpeed2D = direction.Value * speed.Value;
            velocity.ValueRW.Linear = new float3(moveSpeed2D, 0f);

            if (math.abs(moveSpeed2D.x) > 0.15f)
            {
                facingDirection.ValueRW.Value = math.sign(moveSpeed2D.x);
            }

            if (SystemAPI.HasComponent<PlayerTag>(entity))
            {
                var animationOverride = SystemAPI.GetComponentRW<AnimationIndexOverride>(entity);
                var animationType = math.lengthsq(moveSpeed2D) > float.Epsilon
                    ? PlayerAnimationIndex.Movement
                    : PlayerAnimationIndex.Idle;
                animationOverride.ValueRW.Value = (float)animationType;
            }
        }
    }
}

public partial struct GloabalTimeUpdateSystem : ISystem
{
    private static int _globalTimeShaderPropertyID;

    public void OnCreate(ref SystemState state)
    {
        _globalTimeShaderPropertyID = Shader.PropertyToID("_GlobalTime");
    }

    public void OnUpdate(ref SystemState state)
    {
        Shader.SetGlobalFloat(_globalTimeShaderPropertyID, (float)SystemAPI.Time.ElapsedTime);
    }
}