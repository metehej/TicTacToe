using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        //rest of functions are in Game.cs

        #region Click events
        //handles button click
        public void Play_Button_Click(object sender, RoutedEventArgs e) 
        {
            //taking the tag of the sender button and passing it to ClickedEvent() function
            gameInstance.ClickedEvent(Convert.ToInt32(((Button)sender).Tag)); 
        }
        //handles switching between light and dark mode
        private void DayNight_Button_Click(object sender, RoutedEventArgs e) 
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
                //stopping stopwatch
                gameInstance.sw.Stop();
                gameInstance.dt.Stop();
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

                //redrawing grid
                gameInstance.RedrawingPlayfield();
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
            gameInstance.Defaults(false);

        }
        //handles a click on all reset button
        private void ResetAll_Button_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag.ToString() != "popYes")
            {
                popUpShow("Are you sure?", "This action will revert all pictures and settings to default. Do you wish to proceed?", new RoutedEventHandler(ResetAll_Button_Click));
            }
            else
            {
                popUpRemove();
                gameInstance.Defaults(true);
            }
        }
        //handles a click on delete local files button
        private void DeleteLocals_Button_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag.ToString() != "popYes")
            {
                popUpShow("Are you sure?", "This action will delete all saved files and will close the app. Do you wish to proceed?", new RoutedEventHandler(DeleteLocals_Button_Click));
            }
            else
            {
                popUpRemove();
                gameInstance.saveSettings = false;
                if (Directory.Exists(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK"))
                {
                    Directory.Delete(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK", true);
                }
                Close();
            }

        }
        //handles a click on hint or creds buttons
        private void HintCreds_Button_Click(object sender, RoutedEventArgs e)
        {
            if(((Button)sender).Tag.ToString() == "Hint")
            {
                popUpShow("Hint", string.Format("Players switch turns. They place their symbols in the grid, trying to put {0} of their symbols in row. the first one to succeed wins.", gameInstance.InLineForWin), null, "Ok");
            }
            else
            {
                popUpShow("Credits", "Author: Matěj Kretek\nMade for: Gymnázium a SPŠEI Frenštát pod Radhoštěm, p.o ", null, "Ok");
            }
        }
        #endregion

        #region MouseOver events
        //handles mouse hovering on Symbol Change buttons
        private void SymChangeButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            statLine.Text = "Change player's symbol";
        }
        //handles mouse hovering on dayNightButton
        private void DayNight_Button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
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
        //handles click on No/eq button in popup
        private void Popup_No_Button_Click(object sender, RoutedEventArgs e)
        {
            popUpRemove();
        }
        //makes a popup
        public void popUpShow(string header, string text, RoutedEventHandler? yesEvent,  string buttonRight = "No", string buttonLeft = "Yes")
        {
            popButtonGrid.Children.Clear();
            BlurEffect blur = new();
            blur.Radius = 10;
            blur.KernelType = KernelType.Gaussian;
            mainGrid.Effect = blur;
            menuGrid.Effect = blur;
            Panel.SetZIndex(popGrid, 3);
            popGrid.Background = new SolidColorBrush(Color.FromArgb(136, 10, 10, 10));
            popHeader.Text = header;
            popContent.Text = text;
            Button button = new Button();
            button.Click += new RoutedEventHandler(Popup_No_Button_Click);
            button.SetValue(Grid.RowProperty, 1);
            button.SetValue(Grid.ColumnProperty, 3);
            button.SetResourceReference(StyleProperty, "popButton");
            button.Width = 130;
            button.Height = 40;
            button.Content = buttonRight;
            popButtonGrid.Children.Add(button);
            if (yesEvent != null)
            {
                button = new Button();
                button.Click += yesEvent;
                button.Name = "popYes";
                button.SetValue(Grid.RowProperty, 1);
                button.SetValue(Grid.ColumnProperty, 1);
                button.SetResourceReference(StyleProperty, "popButton");
                button.Width = 130;
                button.Height = 40;
                button.Tag = "popYes";
                button.Content = buttonLeft;
                popButtonGrid.Children.Add(button);
            }   
        }
        //removes the popup 
        public void popUpRemove()
        {
            mainGrid.Effect = null;
            menuGrid.Effect = null;
            Panel.SetZIndex(popGrid, 0);
            popGrid.Background = null;
        }
        #endregion
    }
}
