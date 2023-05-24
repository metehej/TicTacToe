using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace tictactoe_forms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#pragma warning disable CS8618
        public static MainWindow Singleton;                             //for communication between MainWindow.xaml.cs and Game.cs
#pragma warning restore CS8618
        gameStatus gameInstance;                                        //creates a Game.cs object
        public MainWindow()
        {
            InitializeComponent();
            //creating singleton
            if (Singleton == null)
            {
                Singleton = this; 
            }
            //creating new game instance
            gameInstance = new();
            //Initialization in Game.cs
            gameInstance.Initialize();
            //setting the context for binding
            DataContext = gameInstance;
        }

        //this file contains functions that are called by gui elements AND are almost entirely GUI related.
        //functions that are both gui and program related are in Game.cs 

        #region Click events
        //handles button click
        public void PlayButtonClick(object sender, RoutedEventArgs e) 
        {
            //taking the tag of the sender button and passing it to ClickedEvent() function
            gameInstance.ClickedEvent(Convert.ToInt32(((Button)sender).Tag)); 
        }
        //handles switching between light and dark mode
        private void DayNightButton_Click(object sender, RoutedEventArgs e) 
        {
            //negating the value of darkMode variable - automatically redraws
            gameInstance.DarkMode = !gameInstance.DarkMode;
            //changing statLine text to correspond new state
            if (gameInstance.DarkMode)
            {
                statLine.Text = "Turn dark mode OFF";
            }
            else
            {
                statLine.Text = "Turn dark mode ON";
            }
        }
        //handles switching AI on/off (WIP)
        private void ButtonAI_Click(object sender, RoutedEventArgs e) 
        {
            Style? style;
            if (gameInstance.Ai)
            {
                gameInstance.Ai = false;
                if (gameInstance.DarkMode)
                {
                    style = gameGrid.FindResource("buttonStyleOffNight") as Style;
                }
                else
                {
                    style = gameGrid.FindResource("buttonStyleOffDay") as Style;
                }

            }
            else
            {
                gameInstance.Ai = true;
                if (gameInstance.DarkMode)
                {
                    style = gameGrid.FindResource("buttonStyleOnNight") as Style;
                }
                else
                {
                    style = gameGrid.FindResource("buttonStyleOnDay") as Style;
                }
            }
            aiButton.Style = style;
        }
        //handles a click on reset button
        private void Reset_Button_Click(object sender, RoutedEventArgs e)
        {
            //calling Restarting() function from Game.cs
            gameInstance.Restarting();
        }
        //handles a click on Menu and Return button
        private void Menu_Button_Click(object sender, RoutedEventArgs e)
        {
            //temporary storing the Day/Night button in a variable
            //explanation: button consists of several elements
            //its moved by moving the outermost element, which is a grid its stored in
            Grid dayNightButtonM = dayNightGrid;
            Grid exitButtonM = exitButtonGrid;
            Grid statLineM = statLineGrid;

            //switching between menu and game UI
            if (Panel.GetZIndex(menuGrid) == 0)
            {
                //(from game to menu)
                //putting the menu UI grid in front of the game one
                Panel.SetZIndex(menuGrid, 2);

                //moving day/night button
                upperGameGrid.Children.Remove(dayNightButtonM);               
                upperMenuGrid.Children.Add(dayNightButtonM);
                //moving exit button
                upperGameGrid.Children.Remove(exitButtonM);
                upperMenuGrid.Children.Add(exitButtonM);
                //moving status line
                mainGrid.Children.Remove(statLineM);
                menuGrid.Children.Add(statLineM);
            }
            else
            {
                //opposite of the previous change
                //(from menu to game)
                Panel.SetZIndex(menuGrid, 0);

                upperMenuGrid.Children.Remove(dayNightButtonM);
                upperGameGrid.Children.Add(dayNightButtonM);

                upperMenuGrid.Children.Remove(exitButtonM);
                upperGameGrid.Children.Add(exitButtonM);

                menuGrid.Children.Remove(statLineM);
                mainGrid.Children.Add(statLineM);
                gameInstance.RedrawingUI();

                //redrawing grid if the gridsize has changed
                if (gameInstance.GridStatus.GetLength(0) != gameInstance.GridSideSize)
                {
                    gameInstance.Restarting();
                }
                else
                {
                    gameInstance.RedrawingPlayfield();
                }
                
            }
        }
        //handles a click on Exit button
        private void Exit_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        //handles a click on Symbol change buttons
        private void SymChangeButton_Click(object sender, RoutedEventArgs e)
        {
            gameInstance.OpenImageFile(((Button)sender).Tag.ToString());
        }
        //handles a click on counters reset button
        private void ResetCount_Button_Click(object sender, RoutedEventArgs e)
        {
            gameInstance.WinsPlayers = new int[2];
            statLine.Text = "Counters were reset.";
        }
        //handles a click on symbols reset button
        private void ResetSym_Button_Click(object sender, RoutedEventArgs e)
        {

        }
        //handles a click on all reset button
        private void ResetAll_Button_Click(object sender, RoutedEventArgs e)
        {

        }
        //handles a click on delete local files button
        private void DeleteLocals_Button_Click(object sender, RoutedEventArgs e)
        {
            gameInstance.saveSettings = false;
            if (Directory.Exists(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK"))
            {
                Directory.Delete(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK", true);
            }
            Close();
        }
        #endregion

        #region MouseOver events
        //handles mouse hovering on Symbol Change buttons
        private void SymChangeButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            statLine.Text = "Change player's symbol";
        }
        //handles mouse hovering on dayNightButton
        private void DayNightButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (gameInstance.DarkMode)
            {
                statLine.Text = "Turn dark mode OFF";
            }
            else
            {
                statLine.Text = "Turn dark mode ON";
            }
        }
        //handes mouse hovering on other Elements (uses Tag as text input)
        private void Element_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            statLine.Text = ((FrameworkElement)sender).Tag.ToString();
        }
        //handles mouse hovering off menu buttons
        private void Element_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (Panel.GetZIndex(menuGrid) == 2)
            {
                statLine.Text = "";
            }
            else
            {
                statLine.SetBinding(TextBlock.TextProperty, gameInstance.statLineBinding);
            }
            
        }
        //handles mouse hovering off name change buttons
        private void NameChange_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            statLine.Text = ""; 
            if (gameInstance.TurnPlayer == 1)
            {
                gameInstance.StatLine = gameInstance.P1Name + "'s turn."; 
            }
            else
            {
                gameInstance.StatLine = gameInstance.P2Name + "'s turn.";
            }
            
        }
        #endregion

        #region other events
        //handles exit operations
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (gameInstance.saveSettings)
            {
                statLine.Text = "Saving, closing...";
                gameInstance.Saving();
            }
        }
        #endregion
    }
}
