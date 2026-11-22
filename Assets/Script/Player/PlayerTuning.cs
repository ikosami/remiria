namespace Pulling
{
    public static class PlayerTuning
    {
        public const float LaunchImpulse = 38f;
        public const float MaxPullDistance = 3.5f;
        public const float GravityScale = 1f;
        public const float FlightLinearDamping = 1.5f;
        public const float MaxFallSpeed = 22f;
        public const float MaxMoveSpeed = 18f;
        public const float Bounciness = 0.65f;
        public const float LowBounciness = 0.35f;
        public const float HighBounciness = Bounciness * 3f;
        public const float BounceFriction = 0.3f;
        public const float LandSpeedThreshold = 5f;
        public const float FallPoseSpeed = 5f;
        public const float StopThreshold = 0.45f;
        public const int StopHoldFrames = 1;
        public const float LaunchGrace = 0.12f;
        public const bool RequireGroundedToStop = true;
        public const float GroundNormalMin = 0.35f;
        public const int TrajectorySteps = 72;
        public const float TrajectoryStep = 0.04f;
        public const int MaxTrajectoryBounces = 5;
        public const float BreakableImpactSpeed = 14f;
        public const float BreakableWeakSpeed = 14f;
        public const float BreakableStrongSpeed = 16f;
        public const int BreakableStrongHits = 2;
        public const float BlastWaveDelay = 0.1f;
        public const float BulletKnockback = 18f;
        public const float EnemyHitSpeed = 8f;
        public const int EnemyBurstCount = 3;
        public const float EnemyShotGap = 0.25f;
        public const float EnemyBurstPause = 1.25f;
        public const float EnemyShotSpeed = 7f / 3f;
        public const float EnemyShotPush = 3.5f;
        public const float EnemyHeavyScale = 2.25f;
        public const float EnemyHeavyPush = 11f;
        public const float EnemyShotLife = 4f;
        public const float EnemyThreeWayAngle = 15f;
        public const float EnemyLaserPause = 5f;
        public const float EnemyLaserPredict = 2.5f;
        public const float EnemyLaserFire = 2.5f;
        public const float EnemyLaserPush = 1.1f;
        public const float MaxHealth = 1000f;
        public const float SunlightLethalSeconds = 5f;
        public const float SunlightRampSeconds = 3f;
        public const float PatchouliSunImmunitySeconds = 20f;
        public const float LaunchTimeScale = 0.2f;
        public const float AirJumpOutlineThickness = 0.06f;
        public const float MeilingPullSeconds = 0.5f;
        public const float MeilingHoldSeconds = 0.5f;
        public const float MeilingLaunchSeconds = 3f;
        public const float MeilingDefaultSpeed = 12f;
        public const float MeilingMinSpeed = 2f;
        public const float MeilingMaxSpeed = 18f;
        public const float MeilingAimRadius = 3f;

        public static float SunlightDamageUpTo(float seconds)
        {
            if (seconds <= 0f)
            {
                return 0f;
            }

            float ramp = SunlightRampSeconds;
            float peak = MaxHealth / (ramp * 0.5f + (SunlightLethalSeconds - ramp));
            if (seconds <= ramp)
            {
                return peak * seconds * seconds / (2f * ramp);
            }

            return peak * (ramp * 0.5f + (seconds - ramp));
        }
    }
}
