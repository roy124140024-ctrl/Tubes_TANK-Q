using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class BotKill : Bot
{   
    // Strategi: Vulture Scavenger (Greedy Proximity + Centrifugal Evasion)
    
    private int? targetId = null; 
    
    // Variabel untuk menyimpan jarak terdekat (sebagai indikator Greedy Choice)
    private double minDistance = double.MaxValue; 
    private Random rnd = new Random(); 

    static void Main(string[] args)
    {
        new BotKill().Start();
    }

    BotKill() : base(BotInfo.FromFile("BotKill.json")) { }

    public override void Run()
    {
        BodyColor = Color.DarkOliveGreen;
        TurretColor = Color.Black;
        RadarColor = Color.DarkGreen;

        while (IsRunning)
        {
            // Mereset memori target setiap putaran radar agar bot tidak mengalami Tunnel Vision
            targetId = null;
            minDistance = double.MaxValue;
            TurnRadarRight(360); 
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double dx = e.X - X;
        double dy = e.Y - Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        // Mengunci musuh yang jaraknya paling dekat (Local Optimum)
        if (targetId == null || distance < minDistance)
        {
            targetId = e.ScannedBotId;
            minDistance = distance;

            double directionToEnemy = DirectionTo(e.X, e.Y);
            
            // Menembak lurus tanpa prediksi agar tidak meleset saat ada tabrakan acak
            double gunTurn = CalcDeltaAngle(GunDirection, directionToEnemy);
            TurnGunRight(gunTurn);

            // Menyesuaikan daya tembak dengan jarak untuk konservasi energi
            if (distance < 150) {
                Fire(3); 
            } else if (distance < 300) {
                Fire(2); 
            } else if (distance < 500) {
                Fire(1);
            }

            // Menambah sudut 105 derajat agar lintasan bergerak melebar menjauhi pusat arena
            double bearingToEnemy = CalcDeltaAngle(Direction, directionToEnemy);
            double turnAngle = bearingToEnemy + 105; 
            TurnRight(turnAngle); 

            // Langkah lincah dan pendek agar radar dapat segera melakukan pemindaian ulang
            if (rnd.Next(2) == 0) {
                Forward(80);
            } else {
                Back(80);
            }
        }
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Mundur menjauhi kerumunan bot
        Back(120);
        TurnRight(90);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Mundur menjauhi tembok
        Back(150);
        TurnRight(120);
    }
}