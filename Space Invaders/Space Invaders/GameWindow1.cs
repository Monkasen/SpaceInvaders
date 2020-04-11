﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input; // For key events

namespace SpaceInvaders {
    public partial class GameWindow : Form {
        private bool musicToggle = true;
        private double gameTicks = 0; // Internal game clock
        private int projectileTick = 0; // Timer for alien projectile animations
        private double speedMultiplier = 1; // Modifies speed of game clock as more aliens are killed
        private int numAliensLeft = 55; // Tracks how many aliens remain
        private int score = 0; // Track the player's close
        private const int rightSideDifference = 88;
        private const int projectileSpeed = 7;
        private int soundStep = 1; // Alien movement sound counter
        private int deathTimer = 0; // Death timer for alien explosion
        private int alienAnimation = 0; // Alien animation step counter
        private bool isGoingRight = true; // Used to check if aliens are going right or not
        private const int AlienPushX = 10; // How far aliens are pushed on the X axis each tick
        private const int AlienPushY = 20; // How far aliens are pushed on the Y axis each tick
        private int totalProjectiles = 0; // Track how many alien projectiles are active
        private int playerLives = 3; // Tracks player lives remaining
        private List<PictureBox> AlienPBList = new List<PictureBox>();
        private List<Alien> AlienList = new List<Alien>();
        private List<Alien> BottomAliens = new List<Alien>();
        private Projectile playerProj = new Projectile(1);
        private Player p1 = new Player();
        private static Random RandomNum = new Random();
        public GameWindow()
        {
            InitializeComponent();
            InitializeAliens(); // Create list of aliens and their graphics
            p1.SetPos(player.Location); // Syncs class and pictureBox
        }

        private void soundToggle_Click(object sender, EventArgs e) // Toggles game sound on/off
        {
            if (musicToggle) {
                musicToggle = false;
                soundToggle.Image = Image.FromFile("resources/textures/Sound_Off.png");
            }
            else {
                musicToggle = true;
                soundToggle.Image = Image.FromFile("resources/textures/Sound_On.png");
            }
        }

        private void alienMovement_Tick(object sender, EventArgs e) // Controls the movement and speed of aliens
        {
            gameTicks = Math.Round((gameTicks * speedMultiplier), 2);
            if (gameTicks >= 15) { // Move aliens after 15 ticks
                PlaySound(1);
                UpdateAliens();
                TryShoot();
                gameTicks = 0; // Reset counter to 0 for next alien movement
            }
            CheckEndGame();
            ++gameTicks;
        }

        private void playerMovement_Tick(object sender, EventArgs e) // Timer handles all player movements and inputs
        {
            if (Keyboard.IsKeyDown(Key.Left) || Keyboard.IsKeyDown(Key.A)) // Move left
            {
                if (player.Location.X > 20) { // Limit movement within game area
                    player.Location = new Point(player.Location.X - 3, player.Location.Y);
                    p1.SetPos(player.Location);
                }
            }
            else if (Keyboard.IsKeyDown(Key.Right) || Keyboard.IsKeyDown(Key.D)) // Move right
            {
                if (player.Location.X < this.Width - rightSideDifference) { // Limit movement within game area
                    player.Location = new Point(player.Location.X + 3, player.Location.Y);
                    p1.SetPos(player.Location);
                }
            }
            if (Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.W)) // Shoot projectile
            {
                if (!p1.IsFired()) {
                    p1.Fire(true);
                    playerProjectile.Location = new Point(p1.GetPos('x') + (25), p1.GetPos('y'));
                    playerProjectile.Visible = playerProj.SetVisibility();
                    playerProj.SetPos(p1.GetPos('x') + 25, 'x');
                    playerProj.SetPos(p1.GetPos('y'), 'y');
                    //projectileCollision.Enabled = true; // Enables collision detection for bullet
                    PlaySound(3);
                }
            }

            if (!p1.IsFired()) return;

            playerProjectile.Location = new Point(playerProj.GetPos('x'), playerProj.GetPos('y') - projectileSpeed);
            playerProj.SetPos(playerProj.GetPos('y') - projectileSpeed, 'y');
            p1.Fire(OutOfBoundsCheck());
        }

        private void projectileCollision_Tick(object sender, EventArgs e) // Checks for player projectile collision with alien
        {
            // Check player's projectile collision
            for (int i = 0; i < 55; i++) {
                if (playerProjectile.Bounds.IntersectsWith(AlienPBList[i].Bounds) && (AlienList[i].GetState() == 1) && p1.IsFired()) { // Checks for bullet intersecting alien, if the alien is alive, and there if there is an active bullet
                    playerProjectile.Visible = playerProj.SetVisibility(); // Hides player's projectile
                    p1.Fire(false); // Disables player's projectile
                    //projectileCollision.Enabled = false; // Disables collision detection for bullet
                    KillAlien(ref i);
                }
            }

            // Check collision for alien projectile
            if (alienProjectile1.Enabled && alienProjectile1.Bounds.IntersectsWith(player.Bounds) ||
                alienProjectile1.Location.Y > this.Size.Height) {
                alienProjectile1.Enabled = false;
                alienProjectile1.Visible = false;
                totalProjectiles--;
            }

            if (alienProjectile2.Enabled && alienProjectile2.Bounds.IntersectsWith(player.Bounds) ||
                alienProjectile2.Location.Y > this.Size.Height) {
                alienProjectile2.Enabled = false;
                alienProjectile2.Visible = false;
                totalProjectiles--;
            }

            if (alienProjectile3.Enabled && alienProjectile3.Bounds.IntersectsWith(player.Bounds) ||
                alienProjectile3.Location.Y > this.Size.Height) {
                alienProjectile3.Enabled = false;
                alienProjectile3.Visible = false;
                totalProjectiles--;
            }

            // Update alien projectiles
            if (alienProjectile1.Enabled)
                alienProjectile1.Location = new Point(alienProjectile1.Location.X, alienProjectile1.Location.Y + (projectileSpeed));
            if (alienProjectile2.Enabled)
                alienProjectile2.Location = new Point(alienProjectile2.Location.X, alienProjectile2.Location.Y + (projectileSpeed));
            if (alienProjectile3.Enabled)
                alienProjectile3.Location = new Point(alienProjectile3.Location.X, alienProjectile3.Location.Y + (projectileSpeed));
        }

        private void objectDeath_Tick(object sender, EventArgs e) // Handles removing alien/player explosion after death
        {
            ++deathTimer;
            if (deathTimer == 10) { // After 10 ticks, remove the explosion and reset the timer
                for (int i = 0; i < 55; i++) { // For every alien
                    if (AlienList[i].GetState() == 0) { // If the alien has a pending death animation...
                        AlienPBList[i].Visible = false; // Disable alien image
                        objectDeath.Enabled = false; // Turn timer off until next death
                    }
                }
                deathTimer = 0; // Reset timer
                objectDeath.Enabled = false;
            }
        }


        private void projectileAnimation_Tick(object sender, EventArgs e) // Handles animations for alien projectiles
        {
            if (projectileTick < 10) {
                alienProjectile1.Image = Image.FromFile("resources/textures/AlienProjectile1_1.png");
                alienProjectile2.Image = Image.FromFile("resources/textures/AlienProjectile2_1.png");
                alienProjectile3.Image = Image.FromFile("resources/textures/AlienProjectile3_1.png");

            }
            else if (projectileTick > 10 && projectileTick < 20) {
                alienProjectile1.Image = Image.FromFile("resources/textures/AlienProjectile1_2.png");
                alienProjectile2.Image = Image.FromFile("resources/textures/AlienProjectile2_2.png");
                alienProjectile3.Image = Image.FromFile("resources/textures/AlienProjectile3_2.png");
            }
            else if (projectileTick > 20 && projectileTick < 30) {
                alienProjectile1.Image = Image.FromFile("resources/textures/AlienProjectile1_3.png");
                alienProjectile2.Image = Image.FromFile("resources/textures/AlienProjectile2_3.png");
                alienProjectile3.Image = Image.FromFile("resources/textures/AlienProjectile3_3.png");

            }
            else if (projectileTick > 30 && projectileTick < 40) {
                alienProjectile1.Image = Image.FromFile("resources/textures/AlienProjectile1_4.png");
                alienProjectile2.Image = Image.FromFile("resources/textures/AlienProjectile2_4.png");
                alienProjectile3.Image = Image.FromFile("resources/textures/AlienProjectile3_4.png");
            }
            if (projectileTick >= 40) {
                projectileTick = 0;
            }
            ++projectileTick;
        }

        private bool OutOfBoundsCheck() // Checks for out of bounds projectile
        {
            // Player projectile check
            if (playerProj.GetPos('y') < 67) {
                playerProjectile.Visible = playerProj.SetVisibility();
                return false;
            }
            else {
                return true;
            }
        }

        private void InitializeAliens() // Builds lists for aliens and their graphics
        {
            // Create and add Alien objects to list
            AlienList.Add(new Alien(3, pbAlien1.Image, pbAlien1.Location.X, pbAlien1.Location.Y));
            AlienList.Add(new Alien(3, pbAlien2.Image, pbAlien2.Location.X, pbAlien2.Location.Y));
            AlienList.Add(new Alien(3, pbAlien3.Image, pbAlien3.Location.X, pbAlien3.Location.Y));
            AlienList.Add(new Alien(3, pbAlien4.Image, pbAlien4.Location.X, pbAlien4.Location.Y));
            AlienList.Add(new Alien(3, pbAlien5.Image, pbAlien5.Location.X, pbAlien5.Location.Y));
            AlienList.Add(new Alien(3, pbAlien6.Image, pbAlien6.Location.X, pbAlien6.Location.Y));
            AlienList.Add(new Alien(3, pbAlien7.Image, pbAlien7.Location.X, pbAlien7.Location.Y));
            AlienList.Add(new Alien(3, pbAlien8.Image, pbAlien8.Location.X, pbAlien8.Location.Y));
            AlienList.Add(new Alien(3, pbAlien9.Image, pbAlien9.Location.X, pbAlien9.Location.Y));
            AlienList.Add(new Alien(3, pbAlien10.Image, pbAlien10.Location.X, pbAlien10.Location.Y));
            AlienList.Add(new Alien(3, pbAlien11.Image, pbAlien11.Location.X, pbAlien11.Location.Y));
            AlienList.Add(new Alien(2, pbAlien12.Image, pbAlien12.Location.X, pbAlien12.Location.Y));
            AlienList.Add(new Alien(2, pbAlien13.Image, pbAlien13.Location.X, pbAlien13.Location.Y));
            AlienList.Add(new Alien(2, pbAlien14.Image, pbAlien14.Location.X, pbAlien14.Location.Y));
            AlienList.Add(new Alien(2, pbAlien15.Image, pbAlien15.Location.X, pbAlien15.Location.Y));
            AlienList.Add(new Alien(2, pbAlien16.Image, pbAlien16.Location.X, pbAlien16.Location.Y));
            AlienList.Add(new Alien(2, pbAlien17.Image, pbAlien17.Location.X, pbAlien17.Location.Y));
            AlienList.Add(new Alien(2, pbAlien18.Image, pbAlien18.Location.X, pbAlien18.Location.Y));
            AlienList.Add(new Alien(2, pbAlien19.Image, pbAlien19.Location.X, pbAlien19.Location.Y));
            AlienList.Add(new Alien(2, pbAlien20.Image, pbAlien20.Location.X, pbAlien20.Location.Y));
            AlienList.Add(new Alien(2, pbAlien21.Image, pbAlien21.Location.X, pbAlien21.Location.Y));
            AlienList.Add(new Alien(2, pbAlien22.Image, pbAlien22.Location.X, pbAlien22.Location.Y));
            AlienList.Add(new Alien(2, pbAlien23.Image, pbAlien23.Location.X, pbAlien23.Location.Y));
            AlienList.Add(new Alien(2, pbAlien24.Image, pbAlien24.Location.X, pbAlien24.Location.Y));
            AlienList.Add(new Alien(2, pbAlien25.Image, pbAlien25.Location.X, pbAlien25.Location.Y));
            AlienList.Add(new Alien(2, pbAlien26.Image, pbAlien26.Location.X, pbAlien26.Location.Y));
            AlienList.Add(new Alien(2, pbAlien27.Image, pbAlien27.Location.X, pbAlien27.Location.Y));
            AlienList.Add(new Alien(2, pbAlien28.Image, pbAlien28.Location.X, pbAlien28.Location.Y));
            AlienList.Add(new Alien(2, pbAlien29.Image, pbAlien29.Location.X, pbAlien29.Location.Y));
            AlienList.Add(new Alien(2, pbAlien30.Image, pbAlien30.Location.X, pbAlien30.Location.Y));
            AlienList.Add(new Alien(2, pbAlien31.Image, pbAlien31.Location.X, pbAlien31.Location.Y));
            AlienList.Add(new Alien(2, pbAlien32.Image, pbAlien32.Location.X, pbAlien32.Location.Y));
            AlienList.Add(new Alien(2, pbAlien33.Image, pbAlien33.Location.X, pbAlien33.Location.Y));
            AlienList.Add(new Alien(1, pbAlien34.Image, pbAlien34.Location.X, pbAlien34.Location.Y));
            AlienList.Add(new Alien(1, pbAlien35.Image, pbAlien35.Location.X, pbAlien35.Location.Y));
            AlienList.Add(new Alien(1, pbAlien36.Image, pbAlien36.Location.X, pbAlien36.Location.Y));
            AlienList.Add(new Alien(1, pbAlien37.Image, pbAlien37.Location.X, pbAlien37.Location.Y));
            AlienList.Add(new Alien(1, pbAlien38.Image, pbAlien38.Location.X, pbAlien38.Location.Y));
            AlienList.Add(new Alien(1, pbAlien39.Image, pbAlien39.Location.X, pbAlien39.Location.Y));
            AlienList.Add(new Alien(1, pbAlien40.Image, pbAlien40.Location.X, pbAlien40.Location.Y));
            AlienList.Add(new Alien(1, pbAlien41.Image, pbAlien41.Location.X, pbAlien41.Location.Y));
            AlienList.Add(new Alien(1, pbAlien42.Image, pbAlien42.Location.X, pbAlien42.Location.Y));
            AlienList.Add(new Alien(1, pbAlien43.Image, pbAlien43.Location.X, pbAlien43.Location.Y));
            AlienList.Add(new Alien(1, pbAlien44.Image, pbAlien44.Location.X, pbAlien44.Location.Y));
            AlienList.Add(new Alien(1, pbAlien45.Image, pbAlien45.Location.X, pbAlien45.Location.Y));
            AlienList.Add(new Alien(1, pbAlien46.Image, pbAlien46.Location.X, pbAlien46.Location.Y));
            AlienList.Add(new Alien(1, pbAlien47.Image, pbAlien47.Location.X, pbAlien47.Location.Y));
            AlienList.Add(new Alien(1, pbAlien48.Image, pbAlien48.Location.X, pbAlien48.Location.Y));
            AlienList.Add(new Alien(1, pbAlien49.Image, pbAlien49.Location.X, pbAlien49.Location.Y));
            AlienList.Add(new Alien(1, pbAlien50.Image, pbAlien50.Location.X, pbAlien50.Location.Y));
            AlienList.Add(new Alien(1, pbAlien51.Image, pbAlien51.Location.X, pbAlien51.Location.Y));
            AlienList.Add(new Alien(1, pbAlien52.Image, pbAlien52.Location.X, pbAlien52.Location.Y));
            AlienList.Add(new Alien(1, pbAlien53.Image, pbAlien53.Location.X, pbAlien53.Location.Y));
            AlienList.Add(new Alien(1, pbAlien54.Image, pbAlien54.Location.X, pbAlien54.Location.Y));
            AlienList.Add(new Alien(1, pbAlien55.Image, pbAlien55.Location.X, pbAlien55.Location.Y));

            // Add pictureboxes to list
            AlienPBList.Add(pbAlien1);
            AlienPBList.Add(pbAlien2);
            AlienPBList.Add(pbAlien3);
            AlienPBList.Add(pbAlien4);
            AlienPBList.Add(pbAlien5);
            AlienPBList.Add(pbAlien6);
            AlienPBList.Add(pbAlien7);
            AlienPBList.Add(pbAlien8);
            AlienPBList.Add(pbAlien9);
            AlienPBList.Add(pbAlien10);
            AlienPBList.Add(pbAlien11);
            AlienPBList.Add(pbAlien12);
            AlienPBList.Add(pbAlien13);
            AlienPBList.Add(pbAlien14);
            AlienPBList.Add(pbAlien15);
            AlienPBList.Add(pbAlien16);
            AlienPBList.Add(pbAlien17);
            AlienPBList.Add(pbAlien18);
            AlienPBList.Add(pbAlien19);
            AlienPBList.Add(pbAlien20);
            AlienPBList.Add(pbAlien21);
            AlienPBList.Add(pbAlien22);
            AlienPBList.Add(pbAlien23);
            AlienPBList.Add(pbAlien24);
            AlienPBList.Add(pbAlien25);
            AlienPBList.Add(pbAlien26);
            AlienPBList.Add(pbAlien27);
            AlienPBList.Add(pbAlien28);
            AlienPBList.Add(pbAlien29);
            AlienPBList.Add(pbAlien30);
            AlienPBList.Add(pbAlien31);
            AlienPBList.Add(pbAlien32);
            AlienPBList.Add(pbAlien33);
            AlienPBList.Add(pbAlien34);
            AlienPBList.Add(pbAlien35);
            AlienPBList.Add(pbAlien36);
            AlienPBList.Add(pbAlien37);
            AlienPBList.Add(pbAlien38);
            AlienPBList.Add(pbAlien39);
            AlienPBList.Add(pbAlien40);
            AlienPBList.Add(pbAlien41);
            AlienPBList.Add(pbAlien42);
            AlienPBList.Add(pbAlien43);
            AlienPBList.Add(pbAlien44);
            AlienPBList.Add(pbAlien45);
            AlienPBList.Add(pbAlien46);
            AlienPBList.Add(pbAlien47);
            AlienPBList.Add(pbAlien48);
            AlienPBList.Add(pbAlien49);
            AlienPBList.Add(pbAlien50);
            AlienPBList.Add(pbAlien51);
            AlienPBList.Add(pbAlien52);
            AlienPBList.Add(pbAlien53);
            AlienPBList.Add(pbAlien54);
            AlienPBList.Add(pbAlien55);

            // Add bottom most aliens to possible alien shooting list
            for (int i = 1; i < 12; i++)
                BottomAliens.Add(AlienList[AlienList.Count - i]);
        }

        private void UpdateAliens() // Handles alien animations, moving the aliens, and updating their position
        {
            #region alien animation
            switch (alienAnimation) {
                case 0: {
                        foreach (var item in AlienList) {
                            if (item.GetAlienType() == 1)
                                item.SetImage(Image.FromFile("resources/textures/Alien1_2.png"));
                            if (item.GetAlienType() == 2)
                                item.SetImage(Image.FromFile("resources/textures/Alien2_2.png"));
                            if (item.GetAlienType() == 3)
                                item.SetImage(Image.FromFile("resources/textures/Alien3_2.png"));
                        }
                        ++alienAnimation;
                        break;
                    }
                case 1: {
                        foreach (var item in AlienList) {
                            if (item.GetAlienType() == 1)
                                item.SetImage(Image.FromFile("resources/textures/Alien1_1.png"));
                            if (item.GetAlienType() == 2)
                                item.SetImage(Image.FromFile("resources/textures/Alien2_1.png"));
                            if (item.GetAlienType() == 3)
                                item.SetImage(Image.FromFile("resources/textures/Alien3_1.png"));
                        }
                        --alienAnimation;
                        break;
                    }
            }

            // Update images
            for (int i = 0; i < 55; i++) {
                if (AlienList[i].GetState() == 1) {
                    AlienPBList[i].Image = AlienList[i].GetImage();
                }
            }
            #endregion
            #region alien movement
            bool noneEdge = true; // Variable checks if any alien made it to edge, if so change Y coord and switch direction

            foreach (var item in AlienPBList) { // Loop checks if any alien that makes it to the edge is alive
                if (item.Location.X > this.Width - rightSideDifference && item.Visible == true) // Check right edge of screen
                {
                    noneEdge = false;
                    break;
                }

                if (item.Location.X >= 20 || !item.Visible) continue;
                noneEdge = false;
                break;
            }

            if (noneEdge) { // If no alien is at an edge then continue as normal
                if (isGoingRight) // Move right
                    foreach (var item in AlienPBList.Where(item => item.Visible))
                        item.Location = new Point(item.Location.X + AlienPushX, item.Location.Y);
                else // Move left
                    foreach (var item in AlienPBList.Where(item => item.Visible))
                        item.Location = new Point(item.Location.X - AlienPushX, item.Location.Y);
            }
            else { // If an alien makes it to the edge, change Y coord and fix their X position
                if (isGoingRight) {
                    foreach (var item in AlienPBList.Where(item => item.Visible))
                        item.Location = new Point(item.Location.X - 10, item.Location.Y + AlienPushY);
                    isGoingRight = false;
                }
                else {
                    foreach (var item in AlienPBList.Where(item => item.Visible))
                        item.Location = new Point(item.Location.X + 10, item.Location.Y + AlienPushY);
                    isGoingRight = true;
                }
            }
            #endregion
            #region update alien position
            for (int i = 0; i < AlienList.Count; i++) {
                AlienList[i].SetXCord(AlienPBList[i].Location.X);
                AlienList[i].SetYCord(AlienPBList[i].Location.Y);
            }
            #endregion
        }

        private void PlaySound(int input) // Handles various game sounds
        {
            if (musicToggle) {
                var sp = new System.Windows.Media.MediaPlayer();
                switch (input) {
                    case 1 when soundStep == 1: { // Alien movement 'music'
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "resources/sounds/tick1.wav");
                            sp.Open(new Uri(path));
                            sp.Play();
                            ++soundStep;
                            break;
                        }
                    case 1 when soundStep == 2: { // Alien movement 'music'
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "resources/sounds/tick2.wav");
                            sp.Open(new Uri(path));
                            sp.Play();
                            ++soundStep;
                            break;
                        }
                    case 1 when soundStep == 3: { // Alien movement 'music'
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "resources/sounds/tick3.wav");
                            sp.Open(new Uri(path));
                            sp.Play();
                            ++soundStep;
                            break;
                        }
                    case 1 when soundStep == 4: { // Alien movement 'music'
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "resources/sounds/tick4.wav");
                            sp.Open(new Uri(path));
                            sp.Play();
                            soundStep = 1;
                            break;
                        }
                    case 2: { // Alien death sound

                            var path = Path.Combine(Directory.GetCurrentDirectory(), "resources/sounds/alienDeath.wav");
                            sp.Open(new Uri(path));
                            sp.Play();
                            break;
                        }
                    case 3: { // Player shoot projectile sound
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "resources/sounds/playerShoot.wav");
                            sp.Open(new Uri(path));
                            sp.Play();
                            break;
                        }
                    case 4: { // Player death sound
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "resources/sounds/playerDeath.wav");
                            sp.Open(new Uri(path));
                            sp.Play();
                            break;
                        }
                }
            }
        }

        private void KillAlien(ref int i) // Handles process of killing an alien
        {
            AlienList[i].SetState(0); // Sets alien state to 'dead'
            AlienPBList[i].Image = Image.FromFile("resources/textures/AlienDeath.png"); // Replaces alien image with death animation
            speedMultiplier *= 1.02; // When an alien dies, increase game speed by 2%
            PlaySound(2); // Play death sound
            playerScore.Text = ($"{score += 10}"); // Add 10 points to score
            --numAliensLeft; // Decrement number of aliens remaining
            objectDeath.Enabled = true; // Starts timer to remove alien explosion

            // DELETE ALL THE COMMENTED OUT CODE LATER IF PROGRAM ENDS UP BEING FINE TO REDUCE CLUTTER
            if (i - 11 > -1) {
                for (int j = i; j > -1; j -= 10) { // Check if preceding aliens are dead
                    if (AlienList[j].GetState() == 1) {
                        BottomAliens[j % 11] = AlienList[i - 11];
                        //MessageBox.Show($"Alien in bottomlist {i % 11} index is now index {i - 11} from AlienList");
                        break;
                    }
                    //if (j - 10 < -1)
                    //  BottomAliens.RemoveAt(i);
                }
                //BottomAliens[i % 11] = AlienList[i - 11];
                //MessageBox.Show($"Alien in bottomlist {i % 11} index is now index {i - 11} from AlienList");
            }
            /*else {
                BottomAliens.RemoveAt(i);
                //MessageBox.Show($"Alien removed at index {i}, now bottomaliens is size {BottomAliens.Count}");
            }*/
        }

        private void CheckEndGame() // Checks for win/lose condition
        {
            if (numAliensLeft == 0) { // If player wins...
                alienMovement.Enabled = false;
                playerMovement.Enabled = false;
                Thread.Sleep(1000);
                GameWindow NewForm = new GameWindow(); // Open new form to start next wave
                NewForm.Show();
                Dispose(false);
            }
            foreach (var item in AlienPBList) { // If aliens reach the end
                if (item.Location.Y > 725 && item.Visible == true) {
                    alienMovement.Enabled = false;
                    playerMovement.Enabled = false;
                    foreach (var item2 in AlienPBList)
                        item2.Visible = false;
                    gameOver.Visible = true;
                }
            }
            if (alienProjectile1.Bounds.IntersectsWith(player.Bounds) || alienProjectile2.Bounds.IntersectsWith(player.Bounds) || alienProjectile3.Bounds.IntersectsWith(player.Bounds)) { // If player is killed by alien projectile
                alienMovement.Enabled = false;
                playerMovement.Enabled = false;
                foreach (var item2 in AlienPBList)
                    item2.Visible = false;
                gameOver.Visible = true;
                player.Visible = false; // Placeholder for death animation
                PlaySound(4);
            }
        }

        private void TryShoot() // Random chance for an alien to shoot 
        {
            for (int i = 0; i < BottomAliens.Count; i++) {
                if ((BottomAliens[i].GetState() == 1) && totalProjectiles <= 3) { // Checks for bullet limit, and if the alien is alive
                    int rand = RandomNum.Next(0, numAliensLeft);
                    if (rand == 1) {
                        int randAlien = RandomNum.Next(0, BottomAliens.Count); // Select random alien in BottomList
                        while (BottomAliens[randAlien].GetState() == 0 && numAliensLeft > 0) // Will keep looping until an alive alien is selected
                            randAlien = RandomNum.Next(0, BottomAliens.Count);
                        if (alienProjectile1.Enabled == false) {
                            alienProjectile1.Location = new Point(BottomAliens[randAlien].GetXCord(), BottomAliens[randAlien].GetYCord());
                            alienProjectile1.Enabled = true;
                            alienProjectile1.Visible = true;
                            ++totalProjectiles;
                            break;
                        }
                        else if (alienProjectile2.Enabled == false) {
                            alienProjectile2.Location = new Point(BottomAliens[randAlien].GetXCord(), BottomAliens[randAlien].GetYCord());
                            alienProjectile2.Enabled = true;
                            alienProjectile2.Visible = true;
                            ++totalProjectiles;
                            break;
                        }
                        else if (alienProjectile3.Enabled == false) {
                            alienProjectile3.Location = new Point(BottomAliens[randAlien].GetXCord(), BottomAliens[randAlien].GetYCord());
                            alienProjectile3.Enabled = true;
                            alienProjectile3.Visible = true;
                            ++totalProjectiles;
                            break;
                        }
                    }
                }
            }
        }
    }
}
