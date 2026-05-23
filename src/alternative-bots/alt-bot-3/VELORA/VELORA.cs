using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class VELORA : Bot
{
    // Strategi Alternatif: Opportunistic Survivalist (Greedy Firepower + Randomized Evasion)
    
    private readonly Random random = new Random();
    private int moveDirection = 1;

    static void Main(string[] args)
    {
        new VELORA().Start();
    }

    VELORA() : base(BotInfo.FromFile("VELORA.json")) { }

    public override void Run()
    {
        BodyColor = Color.Green;   
        TurretColor = Color.Red;  
        RadarColor = Color.Yellow;
        BulletColor = Color.Red;
        ScanColor = Color.Lime;

        // Memutar radar terus-menerus (Infinite Lock) untuk mencari keberadaan musuh
        SetTurnRadarRight(double.PositiveInfinity);

        while (IsRunning)
        {
            // Jika tank sedang diam, bergerak acak untuk mengecoh bidikan lawan
            if (DistanceRemaining == 0)
            {
                SetForward(150 * moveDirection);
                SetTurnRight(random.Next(-60, 60));
            }
            
            // Jika energi kritis (< 30), prioritas mutlak berubah menjadi melarikan diri
            if (Energy < 30)
            {
                SetTurnRight(45);
                SetForward(200 * moveDirection);
            }
            Go(); 
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // Menggunakan fungsi API resmi DirectionTo untuk mencari sudut musuh
        double enemyDirection = DirectionTo(e.X, e.Y);
        double distance = DistanceTo(e.X, e.Y);
        
        // Head-on Targeting: Menyelaraskan meriam dan radar langsung ke posisi musuh saat ini
        SetTurnGunRight(CalcDeltaAngle(GunDirection, enemyDirection));
        SetTurnRadarRight(CalcDeltaAngle(RadarDirection, enemyDirection));

        // Konservasi energi vs Eksekusi instan
        double firePower = 1.0;
        
        // Mengambil keputusan oportunis: Jika musuh sekarat atau jarak sangat dekat, gunakan daya tembak maksimal
        if (e.Energy < 15 || distance < 200) {
            firePower = 3.0;
        } else if (distance < 500) {
            firePower = 2.0;
        } 
        
        if (GunHeat == 0)
        {
            SetFire(firePower); 
        }
        
        // Mundur secara refleks jika musuh terlalu dekat untuk menghindari Ram Damage
        if (distance < 150)
        {
            moveDirection *= -1;
            SetForward(150 * moveDirection);
        }
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Pantulan tajam 90 derajat saat menabrak batas arena
        moveDirection *= -1;
        SetForward(100 * moveDirection);
        SetTurnRight(90);
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Reaksi Acak saat Tertembak: Mengurangi probabilitas tertembak di titik yang sama secara beruntun
        moveDirection *= -1;
        SetTurnRight(random.Next(-90, 90));
        SetForward(150 * moveDirection);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        moveDirection *= -1;
        SetForward(100 * moveDirection);
    }
}