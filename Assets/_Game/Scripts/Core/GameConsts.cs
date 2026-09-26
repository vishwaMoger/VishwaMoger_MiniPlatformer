using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Physics layers (names are registered by the editor setup) and sorting orders.</summary>
    public static class Layers
    {
        public const int Default = 0;
        public const int Ground = 6;
        public const int Player = 7;
        public const int Enemy = 8;
        public const int EnemyHit = 9;
        public const int Hazard = 10;
        public const int Pickup = 11;
        public const int OneWay = 12;
        public const int Projectile = 13;

        public static readonly int SolidMask = (1 << Ground);
        public static readonly int GroundMask = (1 << Ground) | (1 << OneWay);

        public static readonly string[] Names =
        {
            null, null, null, null, null, null, "Ground", "Player", "Enemy", "EnemyHit", "Hazard", "Pickup", "OneWay", "Projectile"
        };

        static bool s_Configured;

        /// <summary>Sets up the 2D collision matrix at runtime so no project settings need hand-editing.</summary>
        public static void ConfigureCollisionMatrix()
        {
            if (s_Configured) return;
            s_Configured = true;
            int[] ours = { Ground, Player, Enemy, EnemyHit, Hazard, Pickup, OneWay, Projectile, Default };
            foreach (var a in ours) foreach (var b in ours) Physics2D.IgnoreLayerCollision(a, b, true);

            void Allow(int a, int b) => Physics2D.IgnoreLayerCollision(a, b, false);
            Allow(Player, Ground); Allow(Player, OneWay); Allow(Player, EnemyHit); Allow(Player, Hazard);
            Allow(Player, Pickup); Allow(Player, Projectile);
            Allow(Enemy, Ground); Allow(Enemy, OneWay);
            Allow(Projectile, Ground);
            Allow(Default, Ground); Allow(Default, OneWay); Allow(Default, Default);

            Physics2D.queriesHitTriggers = false;
            Physics2D.queriesStartInColliders = false;
        }
    }

    public static class Sort
    {
        public const string BackgroundLayer = "Background";
        public const int BackWall = -20;
        public const int Chains = -12;
        public const int Decor = -10;
        public const int Terrain = 0;
        public const int OneWay = 1;
        public const int Traps = 5;
        public const int Items = 8;
        public const int Enemies = 10;
        public const int Player = 20;
        public const int FX = 30;
        public const int Water = 40;
        public const int WorldUI = 50;
    }

    public static class K
    {
        // gameplay tuning shared by several systems
        public const float PPU = 16f;
        public const float CameraSize = 8.4375f; // 1080p / 64px per unit -> 4x pixel scale
    }
}
