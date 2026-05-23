using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class TANK_Q : Bot
{
    // Strategi Utama: Greedy Target Scoring & Multi-Candidate Greedy Movement
    
    private readonly Random random = new Random();
    
    // Variabel state tracking untuk kalkulasi pergerakan dan memori target
    private int targetId = -1;
    private double targetX = double.NaN;
    private double targetY;
    private double targetEnergy = 100;
    private double targetDirection;
    private double targetSpeed;
    private double targetScore = double.NegativeInfinity;
    private int lastSeenTurn = -9999;
    private double moveDirection = 1;
    private int panicUntil = 0;
    private int lastDirectionChange = 0;

    static void Main(string[] args)
    {
        new TANK_Q().Start();
    }

    TANK_Q() : base(BotInfo.FromFile("TANK_Q.json")) { }

    public override void Run()
    {
        BodyColor = Color.FromArgb(30, 30, 30);
        TurretColor = Color.DarkRed;
        RadarColor = Color.OrangeRed;
        BulletColor = Color.Yellow;
        ScanColor = Color.Red;
        TracksColor = Color.Black;
        GunColor = Color.White;

        AdjustGunForBodyTurn = true;
        AdjustRadarForGunTurn = true;
        AdjustRadarForBodyTurn = true;

        while (IsRunning)
        {
            GreedyDirectionShuffle();

            if (HasFreshTarget())
            {
                GreedyRadarLock();
                GreedyMovement();
                GreedyAimAndFire();
            }
            else
            {
                SearchMode();
            }

            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // Menghitung skor objektif setiap musuh yang terdeteksi
        double score = GreedyTargetScore(e);

        // Kunci target jika belum ada target, atau jika skor target baru lebih tinggi dari target saat ini
        if (!HasFreshTarget() || e.ScannedBotId == targetId || score > targetScore)
        {
            // SENSOR PELURU (Bullet Dodging Heuristic)
            if (e.ScannedBotId == targetId)
            {
                double energyDrop = targetEnergy - e.Energy;
                // Jika energi musuh turun 0-3 poin, musuh dipastikan baru saja menembak
                if (energyDrop > 0 && energyDrop <= 3.0)
                {
                    moveDirection *= -1; // Reaksi instan membalik arah
                    panicUntil = TurnNumber + 18;
                    lastDirectionChange = TurnNumber;
                }
            }

            // Update memori state target
            targetId = e.ScannedBotId;
            targetX = e.X;
            targetY = e.Y;
            targetEnergy = e.Energy;
            targetDirection = e.Direction;
            targetSpeed = e.Speed;
            targetScore = score;
            lastSeenTurn = TurnNumber;
        }
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        moveDirection *= -1;
        panicUntil = TurnNumber + 22;
        lastDirectionChange = TurnNumber;
    }

    public override void OnHitWall(HitWallEvent e)
    {
        moveDirection *= -1;
        panicUntil = TurnNumber + 20;
        lastDirectionChange = TurnNumber;
    }

    public override void OnHitBot(HitBotEvent e)
    {
        moveDirection *= -1;
        panicUntil = TurnNumber + 16;

        double gunTurn = GunBearingTo(e.X, e.Y);
        SetTurnGunLeft(gunTurn);

        // Pertahanan jarak dekat saat tabrakan
        if (Math.Abs(gunTurn) < 8 && GunHeat <= 0 && Energy > 6)
        {
            SetFire(2.5);
        }
    }

    private bool HasFreshTarget()
    {
        // Mengecek apakah target masih relevan (tidak hilang dari radar lebih dari 10 turn)
        return !double.IsNaN(targetX) && TurnNumber - lastSeenTurn <= 10;
    }

    private void SearchMode()
    {
        targetId = -1;
        targetScore = double.NegativeInfinity;

        SetTurnRadarLeft(360);
        SetTurnLeft(35 * moveDirection);
        SetForward(120);
    }
    
    // Mengalkulasi bobot ancaman musuh
    private double GreedyTargetScore(ScannedBotEvent e)
    {
        double distance = DistanceTo(e.X, e.Y);

        // Prioritas tinggi untuk musuh yang dekat (closeScore) dan sekarat (weakEnemyScore)
        double closeScore = 700 - distance;
        double weakEnemyScore = 100 - e.Energy;
        double sameTargetBonus = e.ScannedBotId == targetId ? 90 : 0; // Bonus agar tidak terlalu sering ganti target (flicker)

        return closeScore + weakEnemyScore * 3 + sameTargetBonus;
    }
    
    private void GreedyRadarLock()
    {
        double radarTurn = RadarBearingTo(targetX, targetY);
        SetTurnRadarLeft(radarTurn * 2);
    }

    // Memilih sudut pergerakan terbaik dari beberapa kandidat
    private void GreedyMovement()
    {
        double enemyAngle = DirectionTo(targetX, targetY);

        // Himpunan Kandidat Solusi: Sudut-sudut manuver evasi
        double[] candidateAngles =
        {
            enemyAngle + 90,
            enemyAngle - 90,
            enemyAngle + 120,
            enemyAngle - 120,
            enemyAngle + 45,
            enemyAngle - 45,
            DirectionTo(ArenaWidth / 2, ArenaHeight / 2) // Kandidat melarikan diri ke tengah
        };

        double bestAngle = candidateAngles[0];
        double bestScore = double.NegativeInfinity;

        // Evaluasi setiap kandidat untuk mencari Local Optimum (Greedy Choice)
        foreach (double angle in candidateAngles)
        {
            double score = GreedyMoveScore(angle);

            if (score > bestScore)
            {
                bestScore = score;
                bestAngle = angle;
            }
        }

        double moveAmount = TurnNumber < panicUntil ? 185 : 145;
        SmartMove(bestAngle, moveAmount);
    }

    // Mengalkulasi skor kelayakan suatu sudut pergerakan
    private double GreedyMoveScore(double angle)
    {
        double radians = ToRadians(angle);

        // Simulasi prediksi posisi masa depan
        double predictedX = X + Math.Cos(radians) * 125;
        double predictedY = Y + Math.Sin(radians) * 125;
        double score = 0;
        double wallMargin = 80;

        // Penalti mutlak (Fungsi Kelayakan) jika pergerakan akan menabrak tembok
        if (predictedX < wallMargin || predictedY < wallMargin ||
            predictedX > ArenaWidth - wallMargin || predictedY > ArenaHeight - wallMargin)
        {
            score -= 700;
        }
        else
        {
            score += 180;
        }

        // Mencari jarak ideal (tidak terlalu dekat, tidak terlalu jauh dari musuh)
        double predictedDistance = DistanceBetween(predictedX, predictedY, targetX, targetY);
        double idealDistance = 330;
        score -= Math.Abs(predictedDistance - idealDistance);

        // Mendorong pergerakan menyamping (Orbiting) untuk menghindari peluru linier
        double enemyAngle = DirectionTo(targetX, targetY);
        double sideAngle = Math.Abs(NormalizeRelativeAngle(angle - enemyAngle));

        if (TurnNumber < panicUntil)
        {
            if (sideAngle > 70 && sideAngle < 130)
                score += 310;
        }

        double preferredOrbit = enemyAngle + 90 * moveDirection;
        double orbitError = Math.Abs(NormalizeRelativeAngle(angle - preferredOrbit));
        score += 80 - orbitError * 0.4;

        // Menambahkan sedikit variabel acak (Noise) agar pergerakan tidak mudah diprediksi lawan
        score += random.NextDouble() * 25;

        return score;
    }

    // Menghitung lintasan peluru dan prediksi masa depan target
    private void GreedyAimAndFire()
    {
        double distance = DistanceTo(targetX, targetY);
        double firePower = GreedyFirePower(distance);

        if (Energy <= firePower + 1)
            return; // Tidak menembak jika energi kritis

        double bulletSpeed = CalcBulletSpeed(firePower);
        double time = distance / bulletSpeed;

        double predictedX = targetX;
        double predictedY = targetY;

        // Iterasi prediksi posisi masa depan target berdasarkan vektor kecepatannya
        for (int i = 0; i < 2; i++)
        {
            predictedX = targetX + Math.Cos(ToRadians(targetDirection)) * targetSpeed * time;
            predictedY = targetY + Math.Sin(ToRadians(targetDirection)) * targetSpeed * time;

            predictedX = Clamp(predictedX, 18, ArenaWidth - 18);
            predictedY = Clamp(predictedY, 18, ArenaHeight - 18);

            time = DistanceTo(predictedX, predictedY) / bulletSpeed;
        }

        double gunTurn = GunBearingTo(predictedX, predictedY);
        SetTurnGunLeft(gunTurn);

        double aimTolerance;

        // Toleransi akurasi disesuaikan dengan jarak
        if (distance < 180)
            aimTolerance = 8;
        else if (distance < 350)
            aimTolerance = 5;
        else
            aimTolerance = 3;

        // Hanya menembak jika meriam sudah sejajar dengan titik prediksi target
        if (Math.Abs(gunTurn) < aimTolerance && GunHeat <= 0)
        {
            SetFire(firePower);
        }
    }

    // Memilih daya tembak berdasarkan sisa energi musuh dan jarak
    private double GreedyFirePower(double distance)
    {
        if (Energy < 10) return 0.6;
        if (targetEnergy < 8 && distance < 450) return 1.8; // Eksekusi musuh sekarat
        if (distance < 150) return 3.0; // Damage penuh dari jarak dekat
        if (distance < 300) return 2.1;
        if (distance < 500) return 1.3;
        return 0.7; // Peluru ringan untuk jarak jauh
    }

    private void GreedyDirectionShuffle()
    {
        // Mengubah arah pergerakan secara berkala agar tidak terjebak dalam pola orbit monoton
        if (TurnNumber - lastDirectionChange > 24 + random.Next(20))
        {
            moveDirection *= -1;
            lastDirectionChange = TurnNumber;
        }
    }

    private void SmartMove(double wantedAngle, double distance)
    {
        double turn = NormalizeRelativeAngle(wantedAngle - Direction);

        // Jika sudut belok terlalu besar (> 90 derajat), lebih efisien membalikkan arah (mundur)
        if (turn > 90)
        {
            turn -= 180;
            distance *= -1;
        }
        else if (turn < -90)
        {
            turn += 180;
            distance *= -1;
        }

        SetTurnLeft(turn);
        SetForward(distance);
    }

    // UTILITAS MATEMATIKA
    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double DistanceBetween(double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
