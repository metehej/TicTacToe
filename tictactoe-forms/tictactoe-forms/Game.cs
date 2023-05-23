using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using mw = tictactoe_forms.MainWindow;

namespace tictactoe_forms
{

    public class gameStatus : INotifyPropertyChanged
    {

        #region Variable declarations
        private int[,] gridStatus = { { 0 }, { 0 } };                   //the current status of the play grid; format: int[x,y]
        private Tuple<int, int> toChange = Tuple.Create(0, 0);          //last clicked field
        private int[] winsPlayers = { 0, 0 };                           //total number of wins; format: int {p1 wins, p2 wins/ai wins}
        private bool runStopwatch = false;                              //whether the stopwatch should be running or not
        private bool ai, darkMode, end;                                 //for storing states of certain components
        private int turnPlayer = 1, lastTurnPlayer = 1, turns = 0;      //which player is currently playing (1/2), which player was the
                                                                        //first to play this round (1/2), how many turns since start of round
        private int gridSideSize = 0, inLineForWin = 0;                 //size of one side of the grid, how many symbols in line required for win
        private string p1Name = "Player 1", p2Name = "Player 2";        //names of players
        private string statLine = "placeholder";                        //for storing text from statline when writing something temporary over it

        public Binding statLineBinding = new("StatLine");               //binding for statLine element

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
        public bool Ai { get { return ai; } set { ai = value; RedrawingUI(); } }
        public bool DarkMode { get { return darkMode; } set { darkMode = value; RedrawingUI(); } } //Ai and DarkMode automatically redraws certain Ui elements
        public int[,] GridStatus { get { return gridStatus; } set { gridStatus = value; } }
        public int TurnPlayer { get { return turnPlayer; } set { turnPlayer = value; } }
        public int GridSideSize { get { return gridSideSize; } set { gridSideSize = value; if (gridSideSize < inLineForWin) { InLineForWin = gridSideSize; }; OnPropertyChanged("GridSideSize"); } }
        public int InLineForWin { get { return inLineForWin; } set { inLineForWin = value; if (gridSideSize < inLineForWin) { InLineForWin = gridSideSize; }; OnPropertyChanged("InLineForWin"); } }
        public int[] WinsPlayers { get { return winsPlayers; } set { winsPlayers = value; OnPropertyChanged("WinsPlayers"); } }
        public string P1Name { get { return p1Name; } set { p1Name = value; OnPropertyChanged("P1Name"); } }
        public string P2Name { get { return p2Name; } set { p2Name = value; OnPropertyChanged("P2Name"); } }
        public ImageSource? P1Image { get { return p1Image; } set { p1Image = value; OnPropertyChanged("P1Image"); } }
        public ImageSource? P2Image { get { return p2Image; } set { p2Image = value; OnPropertyChanged("P2Image"); } }
        public string StatLine { get { return statLine; } set { statLine = value; OnPropertyChanged("StatLine"); } }
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
                p1ImagePath = "Content/Images/p1.png";
                error = true;
            }
            try
            {
                P1Image = ImageGen(new Uri(p1ImagePath, UriKind.Relative));
            }
            catch (Exception)
            {
                p1ImagePath = "Content/Images/p1.png";
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
                p2ImagePath = "Content/Images/p2.png";
                error = true;
            }
            try
            {
                P2Image = ImageGen(new Uri(p2ImagePath, UriKind.Relative));
            }
            catch (Exception)
            {
                p2ImagePath = "Content/Images/p2.png";
                P2Image = ImageGen(new Uri(p2ImagePath, UriKind.Relative));
                error = true;
            }
            ReadTwoLines();

            //initializing grid related setting and grid itself
            try
            {
                if (tempSetting != null)
                {
                    gridSideSize = Convert.ToInt32(tempSetting);
                    if (gridSideSize < 3 || gridSideSize > 10)
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
            ReadTwoLines();
            if (tempSetting == "1")
            {
                Ai = true;
            }
            else
            {
                Ai = false;
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
            statLineBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            if (error && !firstGame)
            {
                MessageBox.Show("Game failed to load some data from the save file. Affected items were set to default values.", "A small inconvenience appeared!");
            }
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
                button.Click += new RoutedEventHandler(mw.Singleton.PlayButtonClick);
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
                    gridStatus = GridSetter();
                    turns++;
                    //checking if the player won
                    if (WinCheck())
                    {
                        end = true;
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
                        //changing style of buttons to unavailable
                        foreach (Button x in mw.Singleton.playAreaGrid.Children)
                        {
                            x.Style = mw.Singleton.gameGrid.FindResource("buttOff") as Style;
                        }
                        //displaying new scores, de-highlighting symbols
                        mw.Singleton.p1SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(105, 105, 105));
                        mw.Singleton.p2SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(105, 105, 105));


                    }
                    //if no player won, checking if no empty fields left
                    else if (turns == GridSideSize * GridSideSize)
                    {
                        end = true;
                        StatLine = "Its a tie!";
                        RedrawingPlayfield();
                        //changing style of buttons
                        foreach (Button x in mw.Singleton.playAreaGrid.Children)
                        {
                            x.Style = mw.Singleton.gameGrid.FindResource("buttOff") as Style;
                        }
                        //redrawing player symbol boxes
                        mw.Singleton.p1SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(105, 105, 105));
                        mw.Singleton.p2SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(105, 105, 105));
                    }
                    else
                    {
                        turnPlayer = turnPlayer * 2 % 3;
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
        //changes the 2d array according to which button was pressed
        public int[,] GridSetter()
        {
            try
            {
                int[,] stateOfGridNew = gridStatus;
                stateOfGridNew[toChange.Item1, toChange.Item2] = turnPlayer;
                return stateOfGridNew;
            }
            catch (Exception) { }
            return gridStatus;
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
            RedrawingPlayfield();
            mw.Singleton.statLine.Text = "Reset.";
            //after 1 s, displays which player's turn it is. async task allows players to play during this time
            await Task.Delay(1000);
            if (turnPlayer == 1)
            {
                StatLine = p1Name + "'s turn.";
            }
            else
            {
                StatLine = p2Name + "'s turn.";
            }
            if (Panel.GetZIndex(mw.Singleton.menuGrid) < Panel.GetZIndex(mw.Singleton.gameGrid))
            {
                mw.Singleton.statLine.SetBinding(TextBlock.TextProperty, statLineBinding);
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
                //changing the style according to who owns the field and whose turn it is
                if (buttonState != 0)
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
                mw.Singleton.p1SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(187, 189, 83));
                mw.Singleton.p2SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(105, 105, 105));
            }
            else
            {
                mw.Singleton.p2SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(187, 189, 83));
                mw.Singleton.p1SymbolBorder.Background = new SolidColorBrush(Color.FromRgb(105, 105, 105));
            }
        }
        //redraws UI elements
        public void RedrawingUI()
        {
            if (darkMode) 
            {
                //changing the look of:
                //backgrounds
                mw.Singleton.windowGame.Background = new SolidColorBrush(Color.FromRgb(46, 46, 46));
                mw.Singleton.settingsScrollViewer.Background = new SolidColorBrush(Color.FromRgb(70, 70, 70));
                //styles
                mw.Singleton.statLine.Style = mw.Singleton.FindResource("textStyleNight") as Style;
                mw.Singleton.dayNightButton.Style = mw.Singleton.FindResource("buttonStyleNight") as Style;
                if (ai)
                {
                    mw.Singleton.aiButton.Style = mw.Singleton.FindResource("buttonStyleOnNight") as Style;
                }
                else
                {
                    mw.Singleton.aiButton.Style = mw.Singleton.FindResource("buttonStyleOffNight") as Style;
                }
                mw.Singleton.settingsArea.BorderBrush = new SolidColorBrush(Color.FromRgb(223, 223, 223));
                //images
                mw.Singleton.dayNightButtonImage.Source = new BitmapImage(new Uri("Content/Images/moon.png", UriKind.Relative));
                mw.Singleton.menuButtonImage.Source = new BitmapImage(new Uri("Content/Images/menuN.png", UriKind.Relative));
                mw.Singleton.backButtonImage.Source = new BitmapImage(new Uri("Content/Images/returnN.png", UriKind.Relative));
                mw.Singleton.exitButtonImage.Source = new BitmapImage(new Uri("Content/Images/exitN.png", UriKind.Relative));
            }
            else
            {
                //changing the look of:
                //backgrounds
                mw.Singleton.windowGame.Background = new SolidColorBrush(Color.FromRgb(209, 209, 209));
                mw.Singleton.settingsScrollViewer.Background = new SolidColorBrush(Color.FromRgb(185, 185, 185));
                //styles
                mw.Singleton.statLine.Style = mw.Singleton.FindResource("textStyleDay") as Style;
                mw.Singleton.dayNightButton.Style = mw.Singleton.FindResource("buttonStyleDay") as Style;
                if (ai)
                {
                    mw.Singleton.aiButton.Style = mw.Singleton.FindResource("buttonStyleOnDay") as Style;
                }
                else
                {
                    mw.Singleton.aiButton.Style = mw.Singleton.FindResource("buttonStyleOffDay") as Style;
                }
                mw.Singleton.settingsArea.BorderBrush = new SolidColorBrush(Color.FromRgb(0,0,0));
                //images
                mw.Singleton.dayNightButtonImage.Source = new BitmapImage(new Uri("Content/Images/sun.png", UriKind.Relative));
                mw.Singleton.menuButtonImage.Source = new BitmapImage(new Uri("Content/Images/menuD.png", UriKind.Relative));
                mw.Singleton.backButtonImage.Source = new BitmapImage(new Uri("Content/Images/returnD.png", UriKind.Relative));
                mw.Singleton.exitButtonImage.Source = new BitmapImage(new Uri("Content/Images/exitD.png", UriKind.Relative));
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
                "",
                "ai",
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
            if (ai)
            {
                toWrite[19] = "1";
            }
            else
            {
                toWrite[19] = "0";
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
                    string pathString = Environment.GetEnvironmentVariable("appdata") + "\\TicTacToeMK\\p" + System.IO.Path.GetFileName(picDialog.FileName);
                    if (File.Exists(pathString)) File.Delete(pathString);
                    switch (playerNum)
                    {
                        case "1":
                            File.Copy(picDialog.FileName, pathString, true);
                            p1ImagePath = pathString;
                            P1Image = ImageGen(new Uri(p1ImagePath, UriKind.Absolute));
                            break;
                        case "2":
                            File.Copy(picDialog.FileName, pathString, true);
                            p2ImagePath = pathString;
                            P2Image = ImageGen(new Uri(p2ImagePath, UriKind.Absolute));
                            break;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Something went wrong. Exception: " + e.Message, "Yikes.");
                    switch (playerNum)
                    {
                        case "1":
                            p1ImagePath = "Content/Images/p1.png";
                            P1Image = ImageGen(new Uri(p1ImagePath, UriKind.Relative)); 
                            break;
                        case "2":
                            p2ImagePath = "Content/Images/p2.png";
                            P2Image = ImageGen(new Uri(p2ImagePath, UriKind.Relative));
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
        #endregion
    }
}