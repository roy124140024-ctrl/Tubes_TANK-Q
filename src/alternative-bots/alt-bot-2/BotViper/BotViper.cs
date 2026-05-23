using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class BotViper : Bot
{   
    // Strategi: 1v1 Specialist (Reactive Dodging & Linear Predictive Aiming)
    
    // Memori untuk State Tracking (melacak energi musuh pada turn sebelumnya)
    private double previousEnemyEnergy = 100.0; 
    private int moveDirection = 1; 
    private Random rnd = new Random();

    static void Main(string[] args)
    {
        new BotViper().Start();
    }

    BotViper() : base(BotInfo.FromFile("BotViper.json")) { }

    public override void Run()
    {
        BodyColor = Color.DarkCyan;
        TurretColor = Color.Black;
        RadarColor = Color.Cyan;

        while (IsRunning)
        {
            // Radar berputar 360 derajat tiada henti untuk memastikan musuh selalu terkunci di duel 1v1
            TurnRadarRight(360); 
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double directionToEnemy = DirectionTo(e.X, e.Y);
        double dx = e.X - X;
        double dy = e.Y - Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        // Memprediksi arah gerak musuh menggunakan variabel kecepatan (Speed)
        double predictiveGunTurn = CalcDeltaAngle(GunDirection, directionToEnemy + (e.Speed * 0.6));
        TurnGunRight(predictiveGunTurn);
        
        // Daya tembak disesuaikan dengan jarak agar efisien
        if (distance < 200) {
            Fire(3); 
        } else if (distance < 450) {
            Fire(2); 
        } else {
            Fire(1); 
        }

        // Menghitung selisih energi musuh
        double energyDrop = previousEnemyEnergy - e.Energy;
        previousEnemyEnergy = e.Energy; 

        // Memposisikan badan tank selalu tegak lurus (90 derajat) terhadap musuh untuk evasi optimal
        double bearingToEnemy = CalcDeltaAngle(Direction, directionToEnemy);
        TurnRight(bearingToEnemy + 90); 

        // Jika energi musuh turun 0.1 - 3.0 poin, musuh dipastikan baru saja menembak
        if (energyDrop > 0.0 && energyDrop <= 3.0)
        {
            // Langsung putar balik arah untuk mengecoh peluru musuh
            moveDirection *= -1; 

            // Micro-stepping acak agar jarak hindaran tidak mudah diprediksi lawan
            double evadeDistance = 50 + rnd.Next(40);
            if (moveDirection > 0) Forward(evadeDistance);
            else Back(evadeDistance);
        }
        else
        {
            // Pergerakan default jika musuh tidak menembak
            if (rnd.Next(10) > 7) {
                if (moveDirection > 0) Forward(30);
                else Back(30);
            }
        }
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Mundur untuk menjaga jarak ideal duel
        moveDirection *= -1;
        Back(80 * moveDirection);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Memantul menjauh agar tidak terjebak di sudut arena
        moveDirection *= -1;
        Forward(100 * moveDirection);
        TurnRight(90);
    }
}