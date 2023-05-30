using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using mw = tictactoe_forms.MainWindow;

namespace tictactoe_forms
{

    public class gameStatus : INotifyPropertyChanged
    {

        #region Variable declarations
        private int[,] gridStatus = { { 0 }, { 0 } };                   //the current status of the play grid; format: int[x,y]
        private Tuple<int, int> toChange = Tuple.Create(0, 0);          //last clicked field
        private int[] winsPlayers = { 0, 0 };                           //total number of wins; format: int {p1 wins, p2 wins}
        private bool darkMode, end;                                     //for storing states of certain components
        private int turnPlayer = 1, lastTurnPlayer = 1, turns = 0;      //which player is currently playing (1/2), which player was the
                                                                        //first to play this round (1/2), how many turns since start of round
        private int gridSideSize = 0, inLineForWin = 0;                 //size of one side of the grid, how many symbols in line required for win
        private string p1Name = "Player 1", p2Name = "Player 2", currentTime = "placeholder";        
                                                                        //names of players, stopwatch time
        private string statLine = "placeholder";                        //for storing text from statline when writing something temporary over it
        public DispatcherTimer dt = new();
        public Stopwatch sw = new();

        public Binding statLineBinding = new("StatLine");               //binding for statLine element
        public bool saveSettings = true;

        //styles for buttons used in RedrawingPlayfield()
        Style? stylecan;
        Style? stylecant;
        Style? styleown;
        //images for buttons used in RedrawingPlayfield(), default paths to images
        BitmapImage? p0Image;
        string p1ImagePath = "Content/Images/p1.png", p2ImagePath = "Content/Images/p1.png";
        ImageSource? p1Image;
        ImageSource? p2Image;
        #endregion

        #region Access for mainwindow.xaml.cs and propertychagned handler. 
        public bool DarkMode { get { return darkMode; } set { darkMode = value; RedrawingUI(); } } //DarkMode automatically redraws certain Ui elements
        public int TurnPlayer { get { return turnPlayer; } set { turnPlayer = value; } }
        public int GridSideSize { get { return gridSideSize; } set { gridSideSize = value; if (gridSideSize < inLineForWin) { InLineForWin = gridSideSize; }; OnPropertyChanged("GridSideSize"); Restarting(); } }
        public int InLineForWin { get { return inLineForWin; } set { inLineForWin = value; if (gridSideSize < inLineForWin) { GridSideSize = inLineForWin; }; OnPropertyChanged("InLineForWin"); Restarting(); } }
        public int[] WinsPlayers { get { return winsPlayers; } set { winsPlayers = value; OnPropertyChanged("WinsPlayers"); } }
        public string P1Name { get { return p1Name; } set { p1Name = value; OnPropertyChanged("P1Name"); } }
        public string P2Name { get { return p2Name; } set { p2Name = value; OnPropertyChanged("P2Name"); } }
        public ImageSource? P1Image { get { return p1Image; } set { p1Image = value; OnPropertyChanged("P1Image"); } }
        public ImageSource? P2Image { get { return p2Image; } set { p2Image = value; OnPropertyChanged("P2Image"); } }
        public string StatLine { get { return statLine; } set { statLine = value; OnPropertyChanged("StatLine"); } }
        public string CurrentTime { get { return currentTime; } set { currentTime = value; OnPropertyChanged("CurrentTime"); } }
        #endregion

        #region INotifyPropertyChanged
        //handler for binding
        public event PropertyChangedEventHandler? PropertyChanged;
        //notifying UI elements about a change
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Initializations
        //loads game settings and elements according to a save file or default values
        public void Initialize()                                        
        {
            //in case some value from save file is incorrect, shows a message at the end of initialization, won't show if no savefile directory exists
            bool error = false, firstGame = false;
            
            //opening data.txt. if doesnt exist, create an empty file and proceed with default values
            StreamReader saveFile;
            try
            {
                saveFile = new(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK\\data.txt");
            }
            catch (Exception)
            {
                //checking whether the directory exists, creating, if it doesnt
                if (!Directory.Exists(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK"))
                {
                    Directory.CreateDirectory(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK");
                    firstGame = true;
                }
                StreamWriter saveFileW = new(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK\\data.txt");
                saveFileW.Close();
                saveFile = new(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK\\data.txt");
            }
            string? tempSetting;
            ReadTwoLines();

            //setting player scores from the last time
            try
            {
                winsPlayers[0] = Convert.ToInt32(tempSetting);
            }
            catch (Exception)
            {
                winsPlayers[0] = 0;
                error= true;
            }
            ReadTwoLines();
            try
            {
                winsPlayers[1] = Convert.ToInt32(tempSetting);
            }
            catch (Exception)
            {
                winsPlayers[1] = 0;
                error = true;
            }
            ReadTwoLines();

            //setting player Names
            if (tempSetting != null && tempSetting != "")
            {
                p1Name = tempSetting;
            }
            else
            {
                p1Name = "Player 1";
                error= true;
            }
            ReadTwoLines();
            if (tempSetting != null && tempSetting != "")
            {
                p2Name = tempSetting;
            }
            else
            {
                p2Name = "Player 2";
                error = true;
            }
            ReadTwoLines();

            //setting image sources
            p0Image = new BitmapImage(new Uri("Content/Images/p0.png", UriKind.Relative));

            if (tempSetting != "" && tempSetting != null)
            {
                p1ImagePath = tempSetting;
            }
            else
            {
                p1ImagePath = Environment.GetEnvironmentVariable("appdata") + "/TicTacToeMK/" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss_fffffff") + ".png";
                File.Copy(new Uri("Content/Images/p1.png", UriKind.Relative).ToString(), p1ImagePath, true);
                error = true;
            }
            try
            {
                P1Image = ImageGen(new Uri(p1ImagePath, UriKind.Relative));
            }
            catch (Exception)
            {
                p1ImagePath = Environment.GetEnvironmentVariable("appdata") + "/TicTacToeMK/" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss_fffffff") + ".png";
                File.Copy(new Uri("Content/Images/p1.png", UriKind.Relative).ToString(), p1ImagePath, true);
                P1Image = ImageGen(new Uri(p1ImagePath, UriKind.Relative));
                error = true;
            }
            ReadTwoLines();
            if (tempSetting != "" && tempSetting != null)
            {
                p2ImagePath = tempSetting;
            }
            else
            {
                p2ImagePath = Environment.GetEnvironmentVariable("appdata") + "/TicTacToeMK/" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss_fffffff") + ".png";
                File.Copy(new Uri("Content/Images/p2.png", UriKind.Relative).ToString(), p2ImagePath, true);
                error = true;
            }
            try
            {
                P2Image = ImageGen(new Uri(p2ImagePath, UriKind.Absolute));
            }
            catch (Exception)
            {
                p2ImagePath = Environment.GetEnvironmentVariable("appdata") + "/TicTacToeMK/" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss_fffffff") + ".png";
                File.Copy(new Uri("Content/Images/p2.png", UriKind.Relative).ToString(), p2ImagePath, true);
                P2Image = ImageGen(new Uri(p2ImagePath, UriKind.Absolute));
                error = true;
            }
            ReadTwoLines();

            //initializing grid related setting and grid itself
            try
            {
                if (tempSetting != null)
                {
                    gridSideSize = Convert.ToInt32(tempSetting);
                    if (gridSideSize < 3 || gridSideSize > 13)
                    {
                        gridSideSize = 3;
                    }
                }
                else
                {
                    gridSideSize = 3;
                    error = true;
                }
            }
            catch (Exception)
            {
                gridSideSize = 3;
                error = true;
            }
            ReadTwoLines();
            try
            {
                if (tempSetting != null)
                {
                    inLineForWin = Convert.ToInt32(tempSetting);
                    if (inLineForWin > gridSideSize || inLineForWin < 3)
                    {
                        inLineForWin = gridSideSize;
                    }
                }
                else
                {
                    inLineForWin = 3;
                    error = true;
                }
            }
            catch (Exception)
            {
                inLineForWin = gridSideSize;
                error = true;
            }
            ReadTwoLines();

            //setting up initial conditions
            if (tempSetting == "1")
            {
                DarkMode = true;
            }
            else
            {
                DarkMode = false;
            }

            //settings that are not saved into a savefile

            end = false;

            //loading default styles
            stylecan = mw.Singleton.gameGrid.FindResource("playButtonCan") as Style;
            stylecant = mw.Singleton.gameGrid.FindResource("playButtonCant") as Style;
            styleown = mw.Singleton.gameGrid.FindResource("playButtonOwns") as Style;

            //initializing game grid
            gridStatus = GridInitializer(gridSideSize);

            //redrawing the ui according to defaults
            RedrawingPlayfield();

            //Initialization done, starting the game
            StatLine = p1Name + "'s turn.";
            CurrentTime = "00:00:00.00";
            dt.Tick += new EventHandler(StopwatchRunner);
            dt.Interval = new TimeSpan(0, 0, 0, 0, 10);
            statLineBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            if (error && !firstGame)
            {
                MessageBox.Show("Game failed to load some data from the save file. Affected items were set to default values.", "A small inconvenience appeared!");
            }
            saveFile.Close();
            return;
            //reading 2 lines (save file always contains data name and data value on two separate lines)
            void ReadTwoLines()
            {
                tempSetting = saveFile.ReadLine();
                tempSetting = saveFile.ReadLine();
            }
        }
        //creates the game grid
        public int[,] GridInitializer(int size)
        {
            //deletes residual children
            mw.Singleton.playAreaGrid.Children.Clear();
            mw.Singleton.playAreaGrid.Rows = size;
            mw.Singleton.playAreaGrid.Columns = size;

            //creating a var containing a name for the image
            string imageName = "btnImage";

            for (int i = 0; i < Math.Pow(size, 2); i++)
            {
                //creating a button with the style
                Button button = new() { Style = stylecan };
                //setting the Click function
                button.Click += new RoutedEventHandler(mw.Singleton.Play_Button_Click);
                //assigning a tag to the button
                button.Tag = i;
                //creating an image
                Image image = new() { Source = p0Image, Stretch = Stretch.UniformToFill, Margin = new Thickness((16-size)/4.3), Name = imageName + i.ToString() };
                button.Content = image; //adds the image to the button
                mw.Singleton.playAreaGrid.Children.Add(button); //adds the button to the grid
            }
            return new int[size, size];
        }
        #endregion

        #region Game related functions
        //handles a button press
        public void ClickedEvent(int pressedTag)
        {
            //filling "toChange" with indexes x and y of the pressed button
            toChange = Tuple.Create(pressedTag % gridSideSize, pressedTag / gridSideSize);
            //checking for win/tie to prevent any field changes till restart
            if (!end)
            {
                //checking if the field is available
                if (gridStatus[toChange.Item1, toChange.Item2] == 0)
                {
                    //changing the state of the grid (var. "gridStatus")
                    try
                    {
                        int[,] stateOfGridNew = gridStatus;
                        stateOfGridNew[toChange.Item1, toChange.Item2] = turnPlayer;
                        gridStatus = stateOfGridNew;
                    }
                    catch (Exception) { }
                    turns++;
                    //checking if the player won
                    if (WinCheck())
                    {
                        end = true;
                        sw.Stop();
                        dt.Stop();
                        RedrawingPlayfield();
                        if (turnPlayer == 1)
                        {
                            StatLine = p1Name + " won!";
                            WinsPlayers = new int[2] { winsPlayers[0] + 1, winsPlayers[1] };
                        }
                        else
                        {
                            StatLine = p2Name + " won!";
                            WinsPlayers = new int[2] { winsPlayers[0], winsPlayers[1] + 1 };
                        }
                        //displaying new scores, de-highlighting symbols
                        if (turnPlayer == 1)
                        {
                            mw.Singleton.p1SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(200, 255, 200));
                            mw.Singleton.p2SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(85, 85, 95));
                        }
                        else
                        {
                            mw.Singleton.p1SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(85, 85, 95));
                            mw.Singleton.p2SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(200, 255, 200));
                        }
                    }
                    //if no player won, checking if no empty fields left
                    else if (turns == GridSideSize * GridSideSize)
                    {
                        end = true;
                        sw.Stop();
                        dt.Stop();
                        StatLine = "Its a tie!";
                        RedrawingPlayfield();
                        //redrawing player symbol boxes
                        mw.Singleton.p1SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(85, 85, 95));
                        mw.Singleton.p2SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(85, 85, 95));
                    }
                    else
                    {
                        turnPlayer = turnPlayer * 2 % 3;
                        if(!sw.IsRunning)
                        {
                            sw.Start();
                            dt.Start();
                        }
                        RedrawingPlayfield();
                        if (turnPlayer == 1)
                        {
                            StatLine = p1Name + "'s turn.";
                        }
                        else
                        {
                            StatLine = p2Name + "'s turn.";
                        }
                    }

                }
                //for when player clicked an unavailable field
                else
                {
                    StatLine = "Can't insert it here.";
                }
            }
        }
        //checks for win in all possible directions
        public bool WinCheck()
        {
            int x = toChange.Item1;
            int y = toChange.Item2;
            int stopX, stopY, check = 0;
            //checking vertically
            #region
            y = y - inLineForWin + 1;
            if (y < 0) y = 0;
            stopY = toChange.Item2 + inLineForWin;
            if (stopY > gridStatus.GetLength(0)) { stopY = gridStatus.GetLength(0); };
            do
            {
                if (gridStatus[x, y] == turnPlayer)
                {
                    check += 1;
                    if (check == inLineForWin)
                    {
                        return true;
                    }
                }
                else
                {
                    check = 0;
                }
                y++;
            } while (y < stopY);
            check = 0;
            #endregion
            //checking horizontally
            #region
            y = toChange.Item2;
            x = x - inLineForWin + 1;
            if (x < 0) x = 0;
            stopX = toChange.Item1 + inLineForWin;
            if (stopX > gridStatus.GetLength(0)) { stopX = gridStatus.GetLength(0); }
            do
            {
                if (gridStatus[x, y] == turnPlayer)
                {
                    check += 1;
                    if (check == inLineForWin)
                    {
                        return true;
                    }
                }
                else
                {
                    check = 0;
                }
                x++;
            } while (x < stopX);
            check = 0;
            #endregion
            //checking y=-x diagonal
            #region
            x = toChange.Item1 - inLineForWin + 1;
            y = toChange.Item2 - inLineForWin + 1;
            if (x < 0)
            {
                y = y - x;
                x = 0;
            }
            if (y < 0)
            {
                x = x - y;
                y = 0;
            }
            do
            {
                if (gridStatus[x, y] == turnPlayer)
                {
                    check += 1;
                    if (check == inLineForWin)
                    {
                        return true;
                    }
                }
                else
                {
                    check = 0;
                }
                x++;
                y++;
            } while (x < stopX && y < stopY);
            check = 0;
            #endregion
            //checking y=x diagonal
            #region
            stopX = toChange.Item1 - inLineForWin;
            if (stopX < -1) { stopX = -1; }
            x = toChange.Item1;
            y = toChange.Item2;
            for (int i = 1; i < inLineForWin; i++)
            {
                if (y - 1 < 0 || x + 1 == gridStatus.GetLength(0))
                {
                    break;
                }
                y--;
                x++;
            }
            do
            {
                if (gridStatus[x, y] == turnPlayer)
                {
                    check += 1;
                    if (check == inLineForWin)
                    {
                        return true;
                    }
                }
                else
                {
                    check = 0;
                }
                x--;
                y++;
            } while (x > stopX && y < stopY);
            #endregion
            return false;
        }
        //resets the game
        public async void Restarting()
        {
            //resetting grid by changing all its values to zero
            gridStatus = GridInitializer(gridSideSize);
            //choosing starting player
            lastTurnPlayer = lastTurnPlayer * 2 % 3;
            turnPlayer = lastTurnPlayer;
            turns = 0;
            end = false;
            CurrentTime = "00:00:00.00";
            sw.Reset();
            mw.Singleton.stopwatchTextblock.FontSize = 43;
            RedrawingPlayfield();
            mw.Singleton.statLine.Text = "Reset.";
            //after 1 s, displays which player's turn it is. async task allows players to play during this time
            await Task.Delay(1000);
            if (Panel.GetZIndex(mw.Singleton.menuGrid) <= Panel.GetZIndex(mw.Singleton.gameGrid))
            {
                mw.Singleton.statLine.SetBinding(TextBlock.TextProperty, statLineBinding);
            }
            if (turnPlayer == 1)
            {
                StatLine = p1Name + "'s turn.";
            }
            else
            {
                StatLine = p2Name + "'s turn.";
            }
        }
        //works as a stopwatch
        public void StopwatchRunner(object? sender, EventArgs e)
        {
            if(sw.IsRunning)
            {
                TimeSpan timeSpan = sw.Elapsed;
                if(timeSpan.Hours > 99)
                {
                    sw.Stop();
                    dt.Stop();
                    mw.Singleton.stopwatchTextblock.FontSize = 20;
                    CurrentTime = "Taking u so long\nthe author's dead by now.";
                }
                else
                {
                    CurrentTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds / 10);
                }
            }
        }
        #endregion

        #region UI related functions
        //redraws game related elements
        public void RedrawingPlayfield()
        {
            //changing style of each button and the images in them
            foreach (Button x in mw.Singleton.playAreaGrid.Children)
            {
                int buttonState = gridStatus[Convert.ToInt32(x.Tag) % gridSideSize, Convert.ToInt32(x.Tag) / gridSideSize];
                //changing the style according to if the game is over, who owns the field and whose turn it is
                if(end)
                {
                    x.Style = mw.Singleton.gameGrid.FindResource("buttonOff") as Style;
                }
                else if (buttonState != 0)
                {
                    if (buttonState == turnPlayer) { x.Style = styleown; }
                    else { x.Style = stylecant; }
                }
                else { x.Style = stylecan; }
                //refering to the name of the image assigned during grid initialization
                string buttonImageName = "btnImage" + x.Tag.ToString();
                //changing the source of the contained image to the correct player's symbol
                Image? buttonContent = x.Content as Image;
                Binding imageBinding = new();
                imageBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
#pragma warning disable CS8602
                switch (buttonState)
                {
                    case 1:
                        imageBinding.Source = P1Image;
                        buttonContent.SetBinding(Image.SourceProperty, imageBinding);
                        break;
                    case 2:
                        imageBinding.Source = P2Image;
                        buttonContent.SetBinding(Image.SourceProperty, imageBinding);
                        break;
                    default:
                        buttonContent.Source = p0Image;
                        break;
                }
#pragma warning restore CS8602
            }
            //highlighting the background of the symbol box
            if (turnPlayer == 1)
            {
                mw.Singleton.p1SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(210, 210, 222));
                mw.Singleton.p2SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(85, 85, 95));
            }
            else
            {
                mw.Singleton.p1SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(85, 85, 95));
                mw.Singleton.p2SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(210, 210, 222));
            }
        }
        //redraws UI elements
        public void RedrawingUI()
        {
            if (darkMode) 
            {
                //changing the look of:
                //backgrounds
                mw.Singleton.windowGame.Background = new SolidColorBrush(Color.FromRgb(35, 35, 40));
                mw.Singleton.settingsScrollViewer.Background = new SolidColorBrush(Color.FromRgb(45, 45, 50));
                //styles
                mw.Singleton.statLine.Style = mw.Singleton.FindResource("textStyleNight") as Style;
                mw.Singleton.dayNightButton.Style = mw.Singleton.FindResource("buttonStyleNight") as Style;
                mw.Singleton.settingsArea.BorderBrush = new SolidColorBrush(Color.FromRgb(225, 225, 240));
                //images
                mw.Singleton.dayNightButtonImage.Source = new BitmapImage(new Uri("Content/Images/moon.png", UriKind.Relative));
                mw.Singleton.menuButtonImage.Source = new BitmapImage(new Uri("Content/Images/menuN.png", UriKind.Relative));
                mw.Singleton.backButtonImage.Source = new BitmapImage(new Uri("Content/Images/returnN.png", UriKind.Relative));
                mw.Singleton.exitButtonImage.Source = new BitmapImage(new Uri("Content/Images/exitN.png", UriKind.Relative));
                mw.Singleton.hintButtonImage.Source = new BitmapImage(new Uri("Content/Images/hintN.png", UriKind.Relative));
                mw.Singleton.credsButtonImage.Source = new BitmapImage(new Uri("Content/Images/credsN.png", UriKind.Relative));
            }
            else
            {
                //changing the look of:
                //backgrounds
                mw.Singleton.windowGame.Background = new SolidColorBrush(Color.FromRgb(225, 225, 240));
                mw.Singleton.settingsScrollViewer.Background = new SolidColorBrush(Color.FromRgb(210, 210, 222));
                //styles
                mw.Singleton.statLine.Style = mw.Singleton.FindResource("textStyleDay") as Style;
                mw.Singleton.dayNightButton.Style = mw.Singleton.FindResource("buttonStyleDay") as Style;
                mw.Singleton.settingsArea.BorderBrush = new SolidColorBrush(Color.FromRgb(0,0,0));
                //images
                mw.Singleton.dayNightButtonImage.Source = new BitmapImage(new Uri("Content/Images/sun.png", UriKind.Relative));
                mw.Singleton.menuButtonImage.Source = new BitmapImage(new Uri("Content/Images/menuD.png", UriKind.Relative));
                mw.Singleton.backButtonImage.Source = new BitmapImage(new Uri("Content/Images/returnD.png", UriKind.Relative));
                mw.Singleton.exitButtonImage.Source = new BitmapImage(new Uri("Content/Images/exitD.png", UriKind.Relative));
                mw.Singleton.hintButtonImage.Source = new BitmapImage(new Uri("Content/Images/hintD.png", UriKind.Relative));
                mw.Singleton.credsButtonImage.Source = new BitmapImage(new Uri("Content/Images/credsD.png", UriKind.Relative));
            }
        }
        #endregion

        #region File management functions
        //creates a save file data.txt in the folder %appdata%\TicTacToeMK\
        public void Saving()
        {
            StreamWriter saveFile = new StreamWriter(Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK\\data.txt", false);
            string[] toWrite = new string[]
            {
                "p1Count",
                winsPlayers[0].ToString(),
                "p2Count",
                winsPlayers[1].ToString(),
                "p1Name",
                p1Name,
                "p2Name",
                p2Name,
                "p1Image",
                p1ImagePath,
                "p2Image",
                p2ImagePath,
                "gridSideSize",
                gridSideSize.ToString(),
                "inLineForWin",
                inLineForWin.ToString(),
                "darkMode",
                ""
            };
            if (darkMode)
            {
                toWrite[17] = "1";
            }
            else
            {
                toWrite[17] = "0";
            }
            foreach (string x in toWrite)
            {
                saveFile.WriteLine(x);
            }
            saveFile.Close();
        }
        //saves a new symbol, then changes the corresponding image path and bitmap
        public void OpenImageFile(string playerNum)
        {
            OpenFileDialog picDialog = new OpenFileDialog();
            picDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|All files (*.*)|*.*";
            picDialog.Title = "Player " + playerNum;
            picDialog.Multiselect = false;
            picDialog.RestoreDirectory = true;
            picDialog.CheckFileExists = true;
            picDialog.CheckPathExists = true;
            bool? result = picDialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    string pathString = Environment.GetEnvironmentVariable("appdata") + "/TicTacToeMK/" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss_fffffff") + System.IO.Path.GetExtension(picDialog.FileName);
                    switch (playerNum)
                    {
                        case "1":
                            File.Copy(picDialog.FileName, pathString, true);
                            P1Image = ImageGen(new Uri(pathString, UriKind.Absolute));
                            File.Delete(p1ImagePath);
                            p1ImagePath = pathString;
                            break;
                        case "2":
                            File.Copy(picDialog.FileName, pathString, true);
                            P2Image = ImageGen(new Uri(pathString, UriKind.Absolute));
                            File.Delete(p2ImagePath);
                            p2ImagePath = pathString;
                            break;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Something went wrong. Exception: " + e.Message, "Yikes.");
                    switch (playerNum)
                    {
                        case "1":
                            File.Delete(p1ImagePath);
                            p1ImagePath = Environment.GetEnvironmentVariable("appdata") + "/TicTacToeMK/" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss_fffffff") + ".png";
                            File.Copy(new Uri("Content/Images/p1.png", UriKind.Relative).ToString(), p1ImagePath, true);
                            P1Image = ImageGen(new Uri(p1ImagePath, UriKind.Absolute));
                            break;
                        case "2":
                            File.Delete(p2ImagePath);
                            p2ImagePath = Environment.GetEnvironmentVariable("appdata") + "/TicTacToeMK/" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss_fffffff") + ".png";
                            File.Copy(new Uri("Content/Images/p2.png", UriKind.Relative).ToString(), p2ImagePath, true);
                            P2Image = ImageGen(new Uri(p2ImagePath, UriKind.Absolute));
                            break;
                    }
                }
            }
        }
        //loads a BitmapImage without locking the file
        public ImageSource ImageGen(Uri path)
        {
            var returnImage = new BitmapImage();
            returnImage.BeginInit();
            returnImage.UriSource = path;
            returnImage.CacheOption = BitmapCacheOption.OnLoad;
            returnImage.EndInit();
            return returnImage;
        }
        //sets default values
        public void Defaults(bool allDef)
        {
            File.Delete(p1ImagePath);
            p1ImagePath = Environment.GetEnvironmentVariable("appdata") + "/TicTacToeMK/" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss_fffffff") + ".png";
            File.Copy(new Uri("Content/Images/p1.png", UriKind.Relative).ToString(), p1ImagePath, true);
            P1Image = ImageGen(new Uri(p1ImagePath, UriKind.Absolute));
            File.Delete(p2ImagePath);
            p2ImagePath = Environment.GetEnvironmentVariable("appdata") + "/TicTacToeMK/" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss_fffffff") + ".png";
            File.Copy(new Uri("Content/Images/p2.png", UriKind.Relative).ToString(), p2ImagePath, true);
            P2Image = ImageGen(new Uri(p2ImagePath, UriKind.Absolute));
            if(allDef)
            {
                WinsPlayers= new int[]{ 0, 0};
                P1Name = "Player 1";
                P2Name = "Player 2";
                GridSideSize = 3;
                InLineForWin = 3;
                DarkMode = false;
            }
        }
        #endregion
    }
}