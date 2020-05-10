﻿using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Generic;
using NeuralNetwork;
using System.Diagnostics;

namespace DigitRecognizer
{
    public partial class MainWindow : Window
    {
        Point CurrentPoint = new Point();
        Network network;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Rysowanie
        private void CanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                CurrentPoint = e.GetPosition(this);
        }

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Line line = new Line
                {
                    StrokeThickness = 5,
                    Stroke = SystemColors.WindowTextBrush,
                    X1 = CurrentPoint.X,
                    Y1 = CurrentPoint.Y,
                    X2 = e.GetPosition(this).X,
                    Y2 = e.GetPosition(this).Y
                };

                CurrentPoint = e.GetPosition(this);
                PaintSurface.Children.Add(line);
            }
        }
        #endregion

        #region Przyciski
        private void LaunchNetworkButtonClick(object sender, RoutedEventArgs e)
        {
            int MNISTDatasetSizeDivider = 10; // 1 -> 60000+10000; 5 -> 12000+2000; 10 -> 6000+1000; itd.
            double[][][] datasets = Data.PrepareDatasets(MNISTDatasetSizeDivider);
            // datasets[0] - dane wejściowe zbioru treningowego
            // datasets[1] - oczekiwane dane wyjściowe zbioru treningowego
            // datasets[2] - dane wejściowe zbioru walidacyjnego
            // datasets[3] - oczekiwane dane wyjściowe zbioru walidacyjnego

            network = new Network(datasets[0][0].Length, 2, 100, datasets[1][0].Length);
            string tmp = network.LoadWeights("weights.txt");
            if (tmp == "SIEĆ JEST GOTOWA. ")
            {
                MathTextBox.Text = tmp + network.CalculatePrecision(datasets);
                LaunchButton.IsEnabled = false;
                CalculateButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
            }
            else MathTextBox.Text = tmp;
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            PaintSurface.Children.Clear();
            MathTextBox.Text = "WPROWADŹ NOWE DZIAŁANIE W STREFIE RYSOWANIA";
        }

        private void CalculateButtonClick(object sender, RoutedEventArgs e)
        {
            var picture = SaveCanvas(PaintSurface);                                     // Zapisuje canvas w pamięci
            var DigitsInTwoDimensions = DigitDetection.DetectDigits(picture);           // Wywołanie kolejnych funckji do wycinania i obróbki wczytanych cyfr, znaków


            // DO DEBUGOWANIA - SPRAWDZENIA CZY RESIZING SIĘ ZGADZA ITP.:
            for (int i = 0; i < DigitsInTwoDimensions.Count; i++) // dla każdej cyfry
            {
                for (int j = 0; j < DigitsInTwoDimensions[i].Length; j++) // wiersz
                {
                    for (int k = 0; k < DigitsInTwoDimensions[i][j].Length; k++) // kolumna
                        Debug.Write(DigitsInTwoDimensions[i][j][k] + " ");
                    Debug.WriteLine("");
                }
                Debug.WriteLine("");
            }

            List<double[]> digits = Data.RemoveSecondDimensions(DigitsInTwoDimensions); // Przygotowanie pod karmienie sieci 

            string tmp = DigitDetection.RecognizeDigits(digits, network);
            if (tmp != "") MathTextBox.Text = tmp;
            if (!tmp.Contains("NIE"))
            {
                string result = Calculation.Calculate(tmp).ToString();
                if (result == "BŁĘDNY ZAPIS!" || result == "NIE MOŻNA DZIELIĆ PRZEZ ZERO!") MathTextBox.Text = result;
                else MathTextBox.Text += result;
            }
        }
        #endregion

        // Zapis Canvas:
        private MemoryStream SaveCanvas(Canvas canvas)
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)canvas.Width, (int)canvas.Height, 96d, 96d, PixelFormats.Pbgra32);
            
            canvas.Measure(new Size((int)canvas.Width, (int)canvas.Height));
            canvas.Arrange(new Rect(new Size((int)canvas.Width, (int)canvas.Height)));
            renderBitmap.Render(canvas);
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            MemoryStream stream = new MemoryStream();
            encoder.Save(stream);

            return stream;
        }
    }
}