using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FiltrSplotowy;
using Color = System.Drawing.Color;

namespace FiltrSplotowy
{
    public partial class MainWindow : Window
    {
        private float[][] Wartosci;
        private float[][][] WartosciAsync; //please dont hurt me
        private string org;
        private string fin, fin_asyn;
        private string path;
        int threadcount = 8;
        private MyImage obraz;

        int wymiar;

        //# define :<
        private int MAX_THREAD_COUNT = 100;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region wczytywanie

        private string WczytanaLinia(BinaryReader br)
        {
            StringBuilder s = new StringBuilder();
            byte b = 0;
            while (b != 10)
            {
                b = br.ReadByte();
                char c = (char)b;
                s.Append(c);
            }

            return s.ToString().Trim();
        }

        private string WczytajLinie(BinaryReader br)
        {
            string s = WczytanaLinia(br);
            while (s.StartsWith("#") || s.Equals(""))
            {
                s = WczytanaLinia(br);
            }

            return s;
        }



        private void wybierzPlik_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Portable Grayscale Map (*.pgm)|*.pgm";
            if (openFileDialog.ShowDialog() == true)
            {
                Szachownica.IsChecked = false;
                path = openFileDialog.FileName;
                if (Int32.TryParse(watki.Text, out threadcount))
                {
                    if (threadcount < MAX_THREAD_COUNT)
                    {
                        StringBuilder sb = new StringBuilder();

                        if (File.Exists(path))
                        {
                            BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open));
                            //wczytanie wersji pliku pgm
                            string wersja = WczytajLinie(br);
                            sb.AppendLine(wersja);
                            if (!wersja.Equals("P5"))
                            {
                                MessageBox.Show("Nieobslugiwany format pliku PGM - " + wersja);
                                br.Dispose();
                            }
                            else
                            {
                                int width, height;
                                //wczytanie wymiarów obrazu
                                string[] dane = WczytajLinie(br).Split(' ');
                                sb.AppendLine(dane[0] + " " + dane[1]);
                                width = Int32.Parse(dane[0]);
                                height = Int32.Parse(dane[1]);
                                if (height > threadcount - 2)
                                {
                                    //wczytanie maksymalnej wartości koloru piksela w tym pliku
                                    int max = Int32.Parse(WczytajLinie(br));
                                    sb.AppendLine(max.ToString());
                                    if (max > 255)
                                    {
                                        MessageBox.Show("Zbyt duża wartość maksymalna piksela - " + max);
                                        br.Dispose();
                                    }
                                    else
                                    {
                                        obraz = new MyImage(width, height);
                                        float[][] wartosci = inicjalizujTablice(height, width);


                                        byte b = 0;

                                        for (int i = 0; i < height; i++)
                                        {
                                            for (int j = 0; j < width; j++)
                                            {
                                                b = br.ReadByte();
                                                wartosci[i][j] = b;
                                                sb.Append(b + " ");
                                            }

                                            sb.AppendLine();
                                        }


                                        //zapis oryginalu
                                        org = sb.ToString();

                                        obraz.Values = wartosci;
                                        // wczytanie obrazu do wpf
                                        Bitmap poczatkowy = new Bitmap(width, height);
                                        Color c = new Color();
                                        int wartosc;
                                        for (int i = 0; i < height; i++)
                                        {
                                            for (int j = 0; j < width; j++)
                                            {
                                                wartosc = (int)wartosci[i][j];
                                                //ustawienie koloru piksela na odpowiedni odcień szarości
                                                c = Color.FromArgb(wartosc, wartosc, wartosc);
                                                poczatkowy.SetPixel(j, i, c);
                                            }
                                        }

                                        imageORG.Source = BitmapToImageSource(poczatkowy);
                                        br.Dispose();
                                        if (OUTPUT.Text.Length > 0) OUTPUT.Text += '\n';
                                        OUTPUT.Text += "Pomyślnie wyczytano: " + path + '\n';
                                        //
                                        doIt.IsEnabled = true;
                                    }
                                }
                                else
                                {
                                    MessageBox.Show(
                                        "Rozdzielczosc zdjecia jest za niska w stosunku do ilosci watkow");
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Plik nie istnieje!");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Maxymalna liczba watkow to MAX_THREAD_COUNT");
                    }
                }
                else
                {
                    MessageBox.Show("Konwersja liczby wątków nie powiodła się.");
                }
            }
        }

        private void stworzObraz_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            if (Szachownica.IsChecked == true)
            {
                if (Wymiary.Text.Length <= 0)
                {
                    Wymiary.Text = "1024";
                }

                if (Int32.TryParse(Wymiary.Text, out wymiar))
                {
                    if (wymiar > threadcount - 2 && wymiar * wymiar < Int32.MaxValue)
                    {
                        obraz = new MyImage(wymiar, wymiar);

                        int ilePol;
                        if (!Int32.TryParse(BoxSzachownica.Text, out ilePol))
                        {
                            MessageBox.Show("Błędna liczba pól szachownicy.");
                        }
                        else
                        {
                            if (ilePol > wymiar)
                            {
                                MessageBox.Show("Podano zbyt dużą ilość pól");
                            }
                            else
                            {
                                obraz.CreateCheckerboard(ilePol);
                            }
                        }

                        sb.AppendLine("P5");
                        sb.AppendLine(wymiar + " " + wymiar);
                        sb.AppendLine("255");
                        for (int i = 0; i < wymiar; i++)
                        {
                            for (int j = 0; j < wymiar; j++)
                            {
                                sb.Append(obraz.Values[i][j] + " ");
                            }

                            sb.AppendLine();
                        }

                        org += sb.ToString();
                        imageORG.Source = BitmapToImageSource(ImagetoBitMap(obraz));

                        doIt.IsEnabled = true;

                        if (OUTPUT.Text.Length > 0) OUTPUT.Text += '\n';

                        OUTPUT.Text += "Stworzono szachownicę o wymiarach " + Wymiary.Text + "x" +
                                       Wymiary.Text + " i o " + BoxSzachownica.Text + " polach.";
                    }
                    else
                    {
                        MessageBox.Show("Zbyt mały lub duży obraz.");
                    }
                }
                else
                {
                    MessageBox.Show("Błędny rozmiar.");
                }
            }
        }

        #endregion

        #region miniFunkcje

        //dziekuje pan wpf
        public static float[][] inicjalizujTablice(int height, int width)
        {
            float[][] toReturn = new float[height][];

            for (int i = 0; i < height; ++i)
            {
                //for (int j = 0; j < width; j++)
                //{
                //    toReturn[i] = new float[width];
                //    toReturn[i][j] = 0;
                //}

                toReturn[i] = new float[width];


            }

            return toReturn;
        }


        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        //pgm zamieniam na png
        private Bitmap ImagetoBitMap(MyImage mapa)
        {
            int height = mapa.Height;
            int width = mapa.Width;
            Bitmap result = new Bitmap(width, height);
            Color c = new Color();
            int wartosc;
            float[][] wartosci = mapa.Values;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    wartosc = (int)wartosci[i][j];
                    c = Color.FromArgb(wartosc, wartosc, wartosc);
                    result.SetPixel(j, i, c);
                }
            }

            return result;
        }

        private void zapiszObraz_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog openFileDialog = new SaveFileDialog();
            openFileDialog.Filter = "PNG Files (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                ImagetoBitMap(obraz).Save(openFileDialog.FileName);
            }
        }

        private void zapiszTekst_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog openFileDialog = new SaveFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt";
            if (openFileDialog.ShowDialog() == true)
            {
                StreamWriter sw = new StreamWriter(openFileDialog.FileName);

                sw.Write(fin);


                sw.Dispose();
            }
        }

        private void zapiszBinarny_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog openFileDialog = new SaveFileDialog();
            openFileDialog.Filter = "PGM Files (*.pgm)|*.pgm";
            if (openFileDialog.ShowDialog() == true)
            {
                BinaryWriter bw = new BinaryWriter(File.Open(openFileDialog.FileName, FileMode.OpenOrCreate));
                char c = 'a';
                StringBuilder sb = new StringBuilder();
                String tekst = fin;
                //jest to wersja binarna, więc musi mieć oznaczenie P5
                bw.Write('P');
                bw.Write('5');
                bw.Write('\n');
                int wordcount = 3;
                int indeks = 2;
                //ustawienie indeksu na pierwszą liczbę
                while (!Char.IsDigit(tekst[indeks])) indeks++;
                //zapisanie wymiarów i maksymalnej wartości piksela obrazu
                while (wordcount > 0)
                {
                    c = tekst[indeks];
                    bw.Write(c);
                    if (c.Equals('\n') || c.Equals(' ')) wordcount--;
                    indeks++;
                }
                //zapisanie wartości pikseli wyniku
                for (int i = indeks; i < fin.Length; i++)
                {
                    c = tekst[i];
                    //wczytywanie pojedynczych liczb
                    while (!c.Equals('\n') && !c.Equals(' ') && !c.Equals('\r'))
                    {
                        sb.Append(c);
                        i++;
                        c = tekst[i];
                    }
                    if (sb.ToString().Length > 0)
                    {
                        //zapisanie tylko wartości jedności liczby
                        bw.Write(byte.Parse(sb.ToString().Split(',')[0]));
                        sb.Clear();
                    }
                }
                bw.Dispose();
            }
        }

        private void Ktory_Click(object sender, RoutedEventArgs e)
        {
            if (imageNewSyn.Visibility == Visibility.Visible)
            {
                imageNewAsyn.Visibility = Visibility.Visible;
                imageNewSyn.Visibility = Visibility.Hidden;
                Ktory.Content = "Asynchroniczny";
            }
            else
            {
                imageNewSyn.Visibility = Visibility.Visible;
                imageNewAsyn.Visibility = Visibility.Hidden;
                Ktory.Content = "Synchroniczy";
            }
        }

        private void Sprawdz_liczby(object sender, TextCompositionEventArgs e)
        {
            int output;
            if (int.TryParse(e.Text, out output) == false)
            {
                e.Handled = true;
            }
            else
            {
                if (e.Text != "0" && e.Text != "1" && e.Text != "2" && e.Text != "3" && e.Text != "4" &&
                    e.Text != "5" && e.Text != "6" && e.Text != "7" && e.Text != "8" && e.Text != "9")
                {
                    e.Handled = true;
                }
            }
        }

        private void Szachownica_Checked(object sender, RoutedEventArgs e)
        {
            path = "";
            doIt.IsEnabled = false;
            BoxSzachownica.IsEnabled = true;
            stworzObraz.IsEnabled = true;
        }

        private void Szachownica_Unchecked(object sender, RoutedEventArgs e)
        {
            path = "";
            doIt.IsEnabled = false;
            BoxSzachownica.IsEnabled = false;
            stworzObraz.IsEnabled = false;
        }

        #endregion

        #region Magic




        private void doIt_Click(object sender, RoutedEventArgs e)
        {
            Wartosci = obraz.values;

            doIt.IsEnabled = false;
            int iteracje;
            if (!Int32.TryParse(watki.Text, out threadcount))
            {
                MessageBox.Show("Blad przy wczytaniu ilosci watkow.");
            }
            else
            {
                if (threadcount > MAX_THREAD_COUNT || obraz.height < threadcount - 2)
                {
                    MessageBox.Show("Maksymalna liczba watkow to " + MAX_THREAD_COUNT);
                }
                else
                {
                    if (!Int32.TryParse(Iteracje.Text, out iteracje))
                    {
                        MessageBox.Show("Blad przy wczytaniu ilosci iteracji");
                    }
                    else
                    {
                        //float[][] Wartosci;
                        PB.Maximum = 2;
                        PB.Visibility = Visibility.Visible;

                        Stopwatch sw = new Stopwatch();
                        int ile = Int32.Parse(doSredniej.Text);
                        long[] tablica = new long[ile];
                        MyImage przerobiony = obraz;
                        if (singleThreaded.IsChecked == false)
                        {
                            
                            //synchroniczny
                            for (int j = 0; j < ile; ++j)
                            {
                                sw.Restart();
                                przerobiony = obraz;
                                for (int i = 0; i < iteracje; i++)
                                {
                                    przerobiony = przerobiony.convolution();
                                }
                                sw.Stop();
                                tablica[j] = sw.ElapsedMilliseconds;
                            }
                        }

                        //ez
                        
                        this.Dispatcher.Invoke(() =>
                        {
                            PB.Value = 1;
                            ;
                        }, DispatcherPriority.ApplicationIdle);
                        StringBuilder sb = new StringBuilder();
                        int info = 4;
                        int indeks = 0;

                        //wersja, HxW, maxVal
                        if (Szachownica.IsChecked == false)
                        {
                            char c;
                            while (info > 0)
                            {
                                c = org[indeks];
                                sb.Append(c);
                                if (c.Equals('\n') || c.Equals(' ')) info--;
                                indeks++;
                            }
                        }

                        else
                        {
                            sb.AppendLine("P5");
                            sb.AppendLine(wymiar + " " + wymiar); //kwadrat
                            sb.AppendLine("255");
                        }

                        int height = przerobiony.height;
                        int width = przerobiony.width;
                        Wartosci = przerobiony.Values;

                        for (int i = 0; i < height; i++)
                        {
                            for (int j = 0; j < width; j++)
                            {
                                sb.Append(Wartosci[i][j] + " ");
                            }

                            sb.AppendLine();
                        }

                        //zapisanie obrazu stworzonego synchronicznie
                        fin = sb.ToString();

                        imageNewSyn.Source = BitmapToImageSource(ImagetoBitMap(przerobiony));
                        

                        var srednia = tablica.Average();
                        double sumOfSquaresOfDifferences = tablica.Select(val => (val - srednia) * (val - srednia)).Sum();
                        double odchylenie = Math.Sqrt(sumOfSquaresOfDifferences / tablica.Length);
                        srednia = Math.Round(srednia / 1000, 3);
                        odchylenie = Math.Round(odchylenie / 1000, 3);
                        OUTPUT.Text += '\n' + "Synchroniczna: \n" +"Sredni czas: " + srednia + "s.\nOdchylenie: " + odchylenie + "s \n";


                        //wersja asynchroniczna
                        przerobiony = obraz;
                        Wartosci = obraz.Values;

                        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        for (int k = 0; k < ile; ++k)
                        {
                            if (Wybor.SelectedIndex == 0)
                            {
                                sw.Restart();
                                synchronised(iteracje, threadcount, height, width);
                                sw.Stop();
                            }
                            else if (Wybor.SelectedIndex == 1)
                            {
                                sw.Restart();
                                bozeJakieToJestTrudne(height, width, iteracje);
                                sw.Stop();

                                int counter = 0;
                                for (int i = 0; i < WartosciAsync.Length; ++i)
                                {
                                    for (int j = 0; j < WartosciAsync[i].Length; ++j)
                                    {
                                        Wartosci[counter] = WartosciAsync[i][j];
                                        counter++;
                                    }
                                }

                            }
                            else if (Wybor.SelectedIndex == 2)
                            {
                                sw.Restart();
                                syncFor(iteracje, height, width);
                                sw.Stop();
                            }

                            tablica[k] = sw.ElapsedMilliseconds;
                        }
                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        przerobiony.Values = Wartosci;
                        this.Dispatcher.Invoke(() =>
                        {
                            PB.Value = 2;
                            ;
                        }, DispatcherPriority.ApplicationIdle);
                        sb.Clear();
                        info = 4;
                        indeks = 0;
                        //wczytanie wartości uzyskanych w wersji asynchronicznej
                        if (Szachownica.IsChecked == false)
                        {
                            char c;
                            while (info > 0)
                            {
                                c = org[indeks];
                                sb.Append(c);
                                if (c.Equals('\n') || c.Equals(' ')) info--;
                                indeks++;
                            }
                        }
                        else
                        {
                            sb.AppendLine("P5");
                            sb.AppendLine(wymiar + " " + wymiar);
                            sb.AppendLine("255");
                        }

                        for (int i = 0; i < height; i++)
                        {
                            for (int j = 0; j < width; j++)
                            {
                                sb.Append(Wartosci[i][j] + " ");
                            }

                            sb.AppendLine();
                        }

                        fin_asyn = sb.ToString();
                        imageNewAsyn.Source = BitmapToImageSource(ImagetoBitMap(przerobiony));
                        //obraz = przerobiony; //zakomentowany nie zapamietuje stanu przy kolejnych operacjach

                        srednia = tablica.Average();
                        sumOfSquaresOfDifferences = tablica.Select(val => (val - srednia) * (val - srednia)).Sum();
                        odchylenie = Math.Sqrt(sumOfSquaresOfDifferences / tablica.Length);
                        srednia = Math.Round(srednia / 1000, 3);
                        odchylenie = Math.Round(odchylenie / 1000, 3);
                        OUTPUT.Text += '\n' + "Asynchroniczna: \n" + "Sredni czas: " + srednia + "s.\nOdchylenie: " + odchylenie + "s \n";
                        PB.Visibility = Visibility.Hidden;
                        PB.Value = 0;
                        zapiszBinarny.IsEnabled = true;
                        zapiszObraz.IsEnabled = true;
                        zapiszTekst.IsEnabled = true;
                    }
                }
            }

            doIt.IsEnabled = true;
        }


        #region synchro




        public static void inside(object ob)
        {
            float[][] tIN = (float[][])((object[])ob)[0];
            int hStart = (int)((object[])ob)[1];
            int hFin = (int)((object[])ob)[2];
            int wStart = (int)((object[])ob)[3];
            int wFin = (int)((object[])ob)[4];
            CountdownEvent c = (CountdownEvent)((object[])ob)[5];
            float[][] tOUT = (float[][])((object[])ob)[6];





            for (int i = hStart; i < hFin; ++i)
            {

                for (int j = wStart; j < wFin; ++j)
                {
                    float pixel = tIN[i][j] * 0.6f;
                    pixel += tIN[i - 1][j] * 0.1f;
                    pixel += tIN[i + 1][j] * 0.1f;
                    pixel += tIN[i][j - 1] * 0.1f;
                    pixel += tIN[i][j + 1] * 0.1f;
                    tOUT[i][j] = pixel;
                }

            }
            c.Signal();
        }

        public static void outside(object ob)
        {

            float[][] tIN = (float[][])((object[])ob)[0];
            int height = (int)((object[])ob)[1];
            int width = (int)((object[])ob)[2];
            CountdownEvent c = (CountdownEvent)((object[])ob)[3];
            float[][] tOUT = (float[][])((object[])ob)[4];

            for (int i = 1; i < width - 1; i++)
            { // ^- pierwszy wiersz pikseli obrazka
                tOUT[0][i] =
                    tIN[0][i] * 0.6f +
                    tIN[0][i + 1] * 0.1f +
                    tIN[0][i - 1] * 0.1f +
                    tIN[1][i] * 0.1f;
                // v- ostatni wiersz pikseli obrazka
                tOUT[height - 1][i] =
                    tIN[height - 1][i] * 0.6f +
                     tIN[height - 1][i + 1] * 0.1f +
                     tIN[height - 1][i - 1] * 0.1f +
                     tIN[height - 2][i] * 0.1f;
            }
            for (int i = 1; i < height - 1; i++)
            { // <| pierwsza kolumna pikseli obrazka
                tOUT[i][0] =
                    tIN[i][0] * 0.6f +
                    tIN[i][1] * 0.1f +
                    tIN[i + 1][0] * 0.1f +
                    tIN[i - 1][0] * 0.1f;
                // >| ostatnia kolumna pikseli obrazka
                tOUT[i][width - 1] =
                    tIN[i][width - 1] * 0.6f +
                    tIN[i][width - 2] * 0.1f +
                    tIN[i + 1][width - 1] * 0.1f +
                    tIN[i - 1][width - 1] * 0.1f;
            }
            //piksel w lewym górnym rogu obrazka
            tOUT[0][0] =
                    tIN[0][0] * 0.6f +
                    //piksel na prawo
                    tIN[0][1] * 0.1f +
                    //piksel pod nim
                    tIN[1][0] * 0.1f;
            //piksel w prawym górnym rogu
            tOUT[0][width - 1] =
                    tIN[0][width - 1] * 0.6f +
                    //piksel na lewo
                    tIN[0][width - 2] * 0.1f +
                    //piksel pod nim
                    tIN[1][width - 1] * 0.1f;
            //piksel w lewym dolnym rogu
            tOUT[height - 1][0] =
                    tIN[height - 1][0] * 0.6f +
                    //piksel nad nim
                    tIN[height - 2][0] * 0.1f +
                    //piksel na prawo
                    tIN[height - 1][1] * 0.1f;
            //piksel w prawym dolnym rogu
            tOUT[height - 1][width - 1] =
                    tIN[height - 1][width - 1] * 0.6f +
                    //piksel na lewo
                    tIN[height - 1][width - 2] * 0.1f +
                    //piksel nad nim
                    tIN[height - 2][width - 1] * 0.1f;


            c.Signal();
        }
        //float[][] tIN = (float[][])((object[])ob)[0];
        //int hStart = (int)((object[])ob)[1];
        //int hFin = (int)((object[])ob)[2];
        //int wStart = (int)((object[])ob)[3];
        //int wFin = (int)((object[])ob)[4];
        //CountdownEvent c = (CountdownEvent)((object[])ob)[5];
        //float[][] tOUT = (float[][])((object[])ob)[6];
        void synchronised(int przejscia, int watki, int height, int width)
        {


            var countdownEvent = new CountdownEvent(watki);
            var t2 = inicjalizujTablice(height, width);
            int ile = 0;


            if (watki == 2)
            {
                //Thread th1 = new Thread(inside);
                //Thread th2 = new Thread(outside);
                object obj1 = new object[] {Wartosci, 1, height - 1, 1, width - 1, countdownEvent, t2};
                object obj2 = new object[] {Wartosci, height, width, countdownEvent, t2};
                object obj3 = new object[] { t2, 1, height - 1, 1, width - 1, countdownEvent, Wartosci };
                object obj4 = new object[] { t2, height, width, countdownEvent, Wartosci };
                while (ile < przejscia)
                {
                    countdownEvent.Reset();

                    ThreadPool.QueueUserWorkItem(inside,obj1);
                    ThreadPool.QueueUserWorkItem(outside,obj2);
                    countdownEvent.Wait();
                    ++ile;
                    countdownEvent.Reset();
                    ThreadPool.QueueUserWorkItem(inside, obj3);
                    ThreadPool.QueueUserWorkItem(outside, obj4);
                    countdownEvent.Wait();
                    ++ile;

                }

            }

            else if (watki == 5)
            {
                //Thread outsudeThread = new Thread(inside);
                //Thread in1Thread = new Thread(outside);
                //Thread in2Thread = new Thread(inside);
                //Thread in3Thread = new Thread(outside);
                //Thread in4Thread = new Thread(inside);

                object obj1 = new object[] {Wartosci, 1, height / 2, 1, width / 2, countdownEvent, t2};
                object obj2 = new object[] {Wartosci, 1, height / 2, width / 2, width - 1, countdownEvent, t2};
                object obj3 =new object[] { Wartosci, height / 2, height - 1, 1, width / 2, countdownEvent, t2};
                object obj4 =new object[] { Wartosci, height / 2, height - 1, width / 2, width - 1, countdownEvent, t2};
                object out1 = new object[] { Wartosci, height, width, countdownEvent, t2 };
                ////////////
                object obj5 = new object[] { t2, 1, height / 2, 1, width / 2, countdownEvent, Wartosci };
                object obj6 = new object[] { t2, 1, height / 2, width / 2, width - 1, countdownEvent, Wartosci };
                object obj7 = new object[] { t2, height / 2, height - 1, 1, width / 2, countdownEvent, Wartosci };
                object obj8 = new object[] { t2, height / 2, height - 1, width / 2, width - 1, countdownEvent, Wartosci };
               
                object out2= new object[] { t2, height, width, countdownEvent, Wartosci };
                while (ile < przejscia)
                {
                    countdownEvent.Reset();

                    ThreadPool.QueueUserWorkItem(inside, obj1); //TOP LEFT
                    ThreadPool.QueueUserWorkItem(inside, obj2); //TOP RIGHT
                    ThreadPool.QueueUserWorkItem(inside, obj3); // BOT LEFT
                    ThreadPool.QueueUserWorkItem(inside, obj4); // BOT RIGHT
                    ThreadPool.QueueUserWorkItem(outside, out1);
                    countdownEvent.Wait();
                    ++ile;
                    countdownEvent.Reset();
                    ThreadPool.QueueUserWorkItem(inside, obj5); //TOP LEFT
                    ThreadPool.QueueUserWorkItem(inside, obj6); //TOP RIGHT
                    ThreadPool.QueueUserWorkItem(inside, obj7); // BOT LEFT
                    ThreadPool.QueueUserWorkItem(inside, obj8); // BOT RIGHT
                    ThreadPool.QueueUserWorkItem(outside, out2);
                    countdownEvent.Wait();
                    ++ile;

                }



            }

        }


        #endregion

        #region wielkaDepresjaOkropnaImplementacja


        //szybsze od array.copy.to (orzynajmniej przed moimi przerobkami)
        public static float[][] Slice(float[][] source, int start, int end, int width)
        {
            //// Handles negative ends.
            //if (end < 0)
            //{
            //    end = source.Length + end;
            //}
            //int len = end - start;

            // Return new array.
            //T[] res = new T[len];
            //for (int i = 0; i < len; i++)
            //{
            //    res[i] = source[i + start];
            //}
            //return res;
            int len = end - start;
            int newstart = 0;
            float[][] res = new float[len][];

            for (int i = 0; i < len; ++i)
            {
                res[i] = new float[width];
            }

            for (int i = start; i < end; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    res[newstart][j] = source[i][j];
                }

                newstart++;
            }

            return res;
        }

        public void convolution(object ob)
        {
            float[][] tIN = (float[][])((object[])ob)[0];
            int height = (int)((object[])ob)[1];
            int width = (int)((object[])ob)[2];
            float[][] tOUT = (float[][])((object[])ob)[5];
            MyImage img = new MyImage(width, height);
            for (int i = 1; i < height - 1; i++)
            {
                for (int j = 1; j < width - 1; j++)
                {
                    tOUT[i][j] =
                        tIN[i][j] * 0.6f +
                        tIN[i][j + 1] * 0.1f +
                        tIN[i][j - 1] * 0.1f +
                        tIN[i - 1][j] * 0.1f +
                        tIN[i + 1][j] * 0.1f;
                }
            }
            for (int i = 1; i < width - 1; i++)
            { // ^- pierwszy wiersz pikseli obrazka
                tOUT[0][i] =
                    tIN[0][i] * 0.6f +
                    tIN[0][i + 1] * 0.1f +
                    tIN[0][i - 1] * 0.1f +
                    tIN[1][i] * 0.1f;
                // v- ostatni wiersz pikseli obrazka
                tOUT[height - 1][i] =
                    tIN[height - 1][i] * 0.6f +
                    tIN[height - 1][i + 1] * 0.1f +
                    tIN[height - 1][i - 1] * 0.1f +
                    tIN[height - 2][i] * 0.1f;
            }
            for (int i = 1; i < height - 1; i++)
            { // <| pierwsza kolumna pikseli obrazka
                tOUT[i][0] =
                    tIN[i][0] * 0.6f +
                    tIN[i][1] * 0.1f +
                    tIN[i + 1][0] * 0.1f +
                    tIN[i - 1][0] * 0.1f;
                // >| ostatnia kolumna pikseli obrazka
                tOUT[i][width - 1] =
                    tIN[i][width - 1] * 0.6f +
                    tIN[i][width - 2] * 0.1f +
                    tIN[i + 1][width - 1] * 0.1f +
                    tIN[i - 1][width - 1] * 0.1f;
            }
            //piksel w lewym górnym rogu obrazka
            tOUT[0][0] =
                tIN[0][0] * 0.6f +
                //piksel na prawo
                tIN[0][1] * 0.1f +
                //piksel pod nim
                tIN[1][0] * 0.1f;
            //piksel w prawym górnym rogu
            tOUT[0][width - 1] =
                tIN[0][width - 1] * 0.6f +
                //piksel na lewo
                tIN[0][width - 2] * 0.1f +
                //piksel pod nim
                tIN[1][width - 1] * 0.1f;
            //piksel w lewym dolnym rogu
            tOUT[height - 1][0] =
                tIN[height - 1][0] * 0.6f +
                //piksel nad nim
                tIN[height - 2][0] * 0.1f +
                //piksel na prawo
                tIN[height - 1][1] * 0.1f;
            //piksel w prawym dolnym rogu
            tOUT[height - 1][width - 1] =
                tIN[height - 1][width - 1] * 0.6f +
                //piksel na lewo
                tIN[height - 1][width - 2] * 0.1f +
                //piksel nad nim
                tIN[height - 2][width - 1] * 0.1f;

        }

        public void convolutionv2(object ob)
        {
            float[][] tIN = (float[][])((object[])ob)[0];
            int height = (int)((object[])ob)[1];
            int width = (int)((object[])ob)[2];
            int bStart = (int)((object[])ob)[3];
            int bStop = (int)((object[])ob)[4];
            float[][] tOUT = (float[][])((object[])ob)[5];

            for (int i = 1 + bStart; i < height - 1 - bStop; i++)
            {
                for (int j = 1; j < width - 1; j++)
                {
                    tOUT[i][j] =
                        tIN[i][j] * 0.6f +
                        tIN[i][j + 1] * 0.1f +
                        tIN[i][j - 1] * 0.1f +
                        tIN[i - 1][j] * 0.1f +
                        tIN[i + 1][j] * 0.1f;
                }
            }


            for (int i = 1 + bStart; i < height - 1 - bStop; i++)
            { // <| pierwsza kolumna pikseli obrazka
                tOUT[i][0] =
                    tIN[i][0] * 0.6f +
                    tIN[i][1] * 0.1f +
                    tIN[i + 1][0] * 0.1f +
                    tIN[i - 1][0] * 0.1f;
                // >| ostatnia kolumna pikseli obrazka
                tOUT[i][width - 1] =
                    tIN[i][width - 1] * 0.6f +
                    tIN[i][width - 2] * 0.1f +
                    tIN[i + 1][width - 1] * 0.1f +
                    tIN[i - 1][width - 1] * 0.1f;
            }

        }

        public void convolutionv2_TOP(object ob)
        {
            float[][] tIN = (float[][])((object[])ob)[0];
            int height = (int)((object[])ob)[1];
            int width = (int)((object[])ob)[2];
            int bStart = (int)((object[])ob)[3];
            int bStop = (int)((object[])ob)[4];
            float[][] tOUT = (float[][])((object[])ob)[5];

            for (int i = 1 + bStart; i < height - 1 - bStop; i++)
            {
                for (int j = 1; j < width - 1; j++)
                {
                    tOUT[i][j] =
                        tIN[i][j] * 0.6f +
                        tIN[i][j + 1] * 0.1f +
                        tIN[i][j - 1] * 0.1f +
                        tIN[i - 1][j] * 0.1f +
                        tIN[i + 1][j] * 0.1f;
                }
            }
            for (int i = 1; i < width - 1; i++)
            { // ^- pierwszy wiersz pikseli obrazka
                tOUT[0 + bStart][i] =
                    tIN[0 + bStart][i] * 0.6f +
                    tIN[0 + bStart][i + 1] * 0.1f +
                    tIN[0 + bStart][i - 1] * 0.1f +
                    tIN[1 + bStart][i] * 0.1f;

            }
            for (int i = 1 + bStart; i < height - 1 - bStop; i++)
            { // <| pierwsza kolumna pikseli obrazka
                tOUT[i][0] =
                    tIN[i][0] * 0.6f +
                    tIN[i][1] * 0.1f +
                    tIN[i + 1][0] * 0.1f +
                    tIN[i - 1][0] * 0.1f;
                // >| ostatnia kolumna pikseli obrazka
                tOUT[i][width - 1] =
                    tIN[i][width - 1] * 0.6f +
                    tIN[i][width - 2] * 0.1f +
                    tIN[i + 1][width - 1] * 0.1f +
                    tIN[i - 1][width - 1] * 0.1f;
            }
            //piksel w lewym górnym rogu obrazka
            tOUT[0 + bStart][0] =
                tIN[0 + bStart][0] * 0.6f +
                //piksel na prawo
                tIN[0 + bStart][1] * 0.1f +
                //piksel pod nim
                tIN[1 + bStart][0] * 0.1f;
            //piksel w prawym górnym rogu
            tOUT[0 + bStart][width - 1] =
                tIN[0 + bStart][width - 1] * 0.6f +
                //piksel na lewo
                tIN[0 + bStart][width - 2] * 0.1f +
                //piksel pod nim
                tIN[1 + bStart][width - 1] * 0.1f;

        }

        public void convolutionv2_BOTTOM(object ob)
        {
            float[][] tIN = (float[][])((object[])ob)[0];
            int height = (int)((object[])ob)[1];
            int width = (int)((object[])ob)[2];
            int bStart = (int)((object[])ob)[3];
            int bStop = (int)((object[])ob)[4];
            float[][] tOUT = (float[][])((object[])ob)[5];


            for (int i = 1 + bStart; i < height - 1 - bStop; i++)
            {
                for (int j = 1; j < width - 1; j++)
                {
                    tOUT[i][j] =
                        tIN[i][j] * 0.6f +
                        tIN[i][j + 1] * 0.1f +
                        tIN[i][j - 1] * 0.1f +
                        tIN[i - 1][j] * 0.1f +
                        tIN[i + 1][j] * 0.1f;
                }
            }
            for (int i = 1; i < width - 1; i++)
            {
                // v- ostatni wiersz pikseli obrazka
                tOUT[height - 1][i] =
                    tIN[height - 1][i] * 0.6f +
                    tIN[height - 1][i + 1] * 0.1f +
                    tIN[height - 1][i - 1] * 0.1f +
                    tIN[height - 2][i] * 0.1f;
            }
            for (int i = 1 + bStart; i < height - 1 - bStop; i++)
            { // <| pierwsza kolumna pikseli obrazka
                tOUT[i][0] =
                    tIN[i][0] * 0.6f +
                    tIN[i][1] * 0.1f +
                    tIN[i + 1][0] * 0.1f +
                    tIN[i - 1][0] * 0.1f;
                // >| ostatnia kolumna pikseli obrazka
                tOUT[i][width - 1] =
                    tIN[i][width - 1] * 0.6f +
                    tIN[i][width - 2] * 0.1f +
                    tIN[i + 1][width - 1] * 0.1f +
                    tIN[i - 1][width - 1] * 0.1f;
            }

            //piksel w lewym dolnym rogu
            tOUT[height - 1 - bStop][0] =
                tIN[height - 1 - bStop][0] * 0.6f +
                //piksel nad nim
                tIN[height - 2 - bStop][0] * 0.1f +
                //piksel na prawo
                tIN[height - 1 - bStop][1] * 0.1f;
            //piksel w prawym dolnym rogu
            tOUT[height - 1 - bStop][width - 1] =
                tIN[height - 1 - bStop][width - 1] * 0.6f +
                //piksel na lewo
                tIN[height - 1 - bStop][width - 2] * 0.1f +
                //piksel nad nim
                tIN[height - 2 - bStop][width - 1] * 0.1f;
        }

        //float[][] tIN = (float[][])((object[])ob)[0];
        //int height = (int)((object[])ob)[1];
        //int width = (int)((object[])ob)[2];
        //int bStart = (int)((object[])ob)[3];
        //int bStop = (int)((object[])ob)[4];
        //float[][] tOUT = (float[][])((object[])ob)[5];
        private void magik(object dane)
        {
            int[] extraInfo = new int[3];
            extraInfo[1] = (int)((object[])dane)[4]; // mid top bot -> czesc obrazka
            extraInfo[0] = (int)((object[])dane)[7]; // ile nie usuwac gory
            extraInfo[2] = (int)((object[])dane)[8]; //ile nie usuwac dolu
            float[][][] kontener = new float[2][][];
            int height = (int)((object[])dane)[0];
            int width = (int)((object[])dane)[1];
            kontener[0] = Slice((float[][])((object[])dane)[2], 0, height, width); // część
            int iteracje = (int)((object[])dane)[3];
            CountdownEvent c = (CountdownEvent)((object[])dane)[5];
            int ktory = (int)((object[])dane)[6];
            int counter = 0;
            int bonusStart = 0;
            int bonusStop = 0;
            kontener[1] = inicjalizujTablice(height, width);

            //normalny przypadek
            if (extraInfo[0] <= 0 && extraInfo[2] <= 0)
            {
                if (extraInfo[1] == 0) // normalna czesc obrazka
                {
                    for (int i = 0; i < iteracje; ++i)
                    {
                        convolutionv2(new object[] { kontener[counter % 2], height, width, bonusStart, bonusStop, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolutionv2();
                        //tmp.Values = Slice(tmp.Values, 1, tmp.height - 1, tmp.width);
                        //tmp.height = tmp.height - 2;
                        ++bonusStart;
                        ++bonusStop;
                        ++counter;
                    }
                }

                if (extraInfo[1] == 1) // gorna czesc obrazka
                {
                    for (int i = 0; i < iteracje; ++i)
                    {
                        convolutionv2_TOP(new object[] { kontener[counter % 2], height, width, bonusStart, bonusStop, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolutionv2_TOP();
                        //tmp.Values = Slice(tmp.Values, 0, tmp.height - 1, tmp.width);
                        //tmp.height = tmp.height - 1;
                        ++bonusStop;
                        ++counter;
                    }
                }

                if (extraInfo[1] == 2) // dolna czesc obrazka
                {
                    for (int i = 0; i < iteracje; ++i)
                    {
                        convolutionv2_BOTTOM(new object[] { kontener[counter % 2], height, width, bonusStart, bonusStop, kontener[-1 * (counter % 2 - 1)] });
                        // tmp = tmp.convolutionv2_BOTTOM();
                        //tmp.Values = Slice(tmp.Values, 1, tmp.height, tmp.width);
                        //tmp.height = tmp.height - 1;
                        ++bonusStart;
                        ++counter;
                    }
                }
            }
            else if (extraInfo[0] <= extraInfo[2])
            {
                int j = 0;
                if (extraInfo[1] == 0) // normalna czesc obrazka
                {
                    for (; iteracje > 0 && j < extraInfo[0]; --iteracje, ++j)
                    {
                        convolution(new object[] { kontener[counter % 2], height, width, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolution();
                        ++counter;
                    }


                    for (; iteracje > 0 && j < extraInfo[2]; --iteracje, ++j)
                    {
                        convolutionv2_BOTTOM(new object[] { kontener[counter % 2], height, width, bonusStart, bonusStop, kontener[-1 * (counter % 2 - 1)] });
                        // tmp = tmp.convolutionv2_BOTTOM();
                        //tmp.Values = Slice(tmp.Values, 1, tmp.height, tmp.width);
                        //tmp.height = tmp.height - 1;
                        ++bonusStart;
                        ++counter;
                    }


                    for (int i = 0; i < iteracje; ++i)
                    {
                        convolutionv2(new object[] { kontener[counter % 2], height, width, bonusStart, bonusStop, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolutionv2();
                        //tmp.Values = Slice(tmp.Values, 1, tmp.height - 1, tmp.width);
                        //tmp.height = tmp.height - 2;
                        ++bonusStart;
                        ++bonusStop;
                        ++counter;
                    }
                }
                else
                {
                    for (; iteracje > 0 && j < extraInfo[2]; --iteracje, ++j)
                    {
                        convolution(new object[] { kontener[counter % 2], height, width, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolution();
                        ++counter; // mamy do czynienia z gorna czescia na zasadach synchronicznych
                    }


                    for (int i = 0; i < iteracje; ++i)
                    {
                        convolutionv2_TOP(new object[] { kontener[counter % 2], height, width, bonusStart, bonusStop, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolutionv2_TOP();
                        //tmp.Values = Slice(tmp.Values, 0, tmp.height - 1, tmp.width);
                        //tmp.height = tmp.height - 1;
                        ++bonusStop;
                        ++counter;
                    }
                }
            }
            else
            {
                int j = 0;
                if (extraInfo[1] == 0) // normalna czesc obrazka
                {
                    for (; iteracje > 0 && j < extraInfo[2]; --iteracje, ++j)
                    {
                        convolution(new object[] { kontener[counter % 2], height, width, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolution();
                        ++counter;
                    }


                    for (; iteracje > 0 && j < extraInfo[0]; --iteracje, ++j)
                    {
                        convolutionv2_TOP(new object[] { kontener[counter % 2], height, width, bonusStart, bonusStop, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolutionv2_TOP();
                        //tmp.Values = Slice(tmp.Values, 0, tmp.height - 1, tmp.width);
                        //tmp.height = tmp.height - 1;
                        ++bonusStop;
                        ++counter;
                    }


                    for (int i = 0; i < iteracje; ++i)
                    {
                        convolutionv2(new object[] { kontener[counter % 2], height, width, bonusStart, bonusStop, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolutionv2();
                        //tmp.Values = Slice(tmp.Values, 1, tmp.height - 1, tmp.width);
                        //tmp.height = tmp.height - 2;
                        ++bonusStart;
                        ++bonusStop;
                        ++counter;
                    }
                }
                else
                {
                    for (; iteracje > 0 && j < extraInfo[0]; --iteracje, ++j)
                    {
                        convolution(new object[] { kontener[counter % 2], height, width, kontener[-1 * (counter % 2 - 1)] });
                        //tmp = tmp.convolution();
                        ++counter;
                    }


                    for (int i = 0; i < iteracje; ++i)
                    {
                        convolutionv2_BOTTOM(new object[] { kontener[counter % 2], height, width, bonusStart, bonusStop, kontener[-1 * (counter % 2 - 1)] });
                        // tmp = tmp.convolutionv2_BOTTOM();
                        //tmp.Values = Slice(tmp.Values, 1, tmp.height, tmp.width);
                        //tmp.height = tmp.height - 1;
                        ++bonusStart;
                        ++counter;
                    }
                }
            }


            WartosciAsync[ktory] = Slice(kontener[counter % 2], 0 + bonusStart, height - bonusStop, width);
            c.Signal();
        }





        private void bozeJakieToJestTrudne(int height, int width, int iteracje)
        {
            WartosciAsync = new float[threadcount][][];

            var countdownEvent = new CountdownEvent(threadcount);
            ////////////////////////////////////////////////////////////////////


            int newHeight = height / threadcount;
            int x = height % threadcount;
            int z = x / 2;
            int y = x % 2; // jezeli nie jest mozliwe podzielenie bez reszty wartosc z to dodatkowe pola w funkcji dolu, natomiast z to dodatkowe pola w funkcji gory ,wieksze o 1 w razie braku parzystosci
            int newTopHeight = newHeight + z + y;
            int newBottomHeight = newHeight + z;
            int bonus = 0;



            int zero = 0;
            int jeden = 1; //top
            int dwa = iteracje + newTopHeight - height;
            if (dwa < 1) bonus = iteracje;
            else bonus = height - newTopHeight;

            ThreadPool.QueueUserWorkItem(magik,
                new object[]
                {
                    newTopHeight + bonus, width, Slice(Wartosci, 0, newTopHeight + bonus, width), iteracje,
                    jeden, countdownEvent, 0,zero,dwa
                });

            zero = iteracje + newBottomHeight - height;
            jeden = 2; //bot
            dwa = 0;
            if (zero < 1)
                bonus = iteracje; // nie trzeba nie ucinac == mozna zaczac wczesniej bez problemu
            else bonus = height - newBottomHeight; // jezeli trzeba to musimy zaczac od poczatku obrazu
            ThreadPool.QueueUserWorkItem(magik,
                new object[]
                {
                    newBottomHeight + bonus, width,
                    Slice(Wartosci, height - newBottomHeight - bonus, height, width), iteracje, jeden,
                    countdownEvent, threadcount - 1, zero,dwa
                });

            jeden = 0;
            for (int i = 0; i < threadcount - 2; i++)
            {
                int dostepneWyzej =
                    newTopHeight + (i * newHeight); // Ile pol na obrazku istnieje nad domyslnym starterem
                int dostepneNizej = newBottomHeight + ((threadcount - 3 - i) * newHeight);
                // extrainfo[0] = nie usuwanie z gory, [2] = nieusuwanie z dolu
                zero = iteracje - dostepneWyzej;
                dwa = iteracje - dostepneNizej;

                int starting = dostepneWyzej; //6 pol nad oznacza start jako float[6][]
                int ending = dostepneWyzej + newHeight;
                if (zero < 1)
                    starting =
                        starting - iteracje; // nie trzeba nie ucinac == mozna zaczac wczesniej bez problemu
                else starting = 0; // jezeli trzeba to musimy zaczac od poczatku obrazu
                if (dwa < 1) ending = ending + iteracje;
                else ending = height;
                ThreadPool.QueueUserWorkItem(magik,
                    new object[]
                    {
                        ending - starting, width, Slice(Wartosci, starting, ending, width), iteracje,
                        jeden, countdownEvent, 1 + i, zero,dwa
                    }); // pomijamy gore i poprzednie czescci w slice, fajnie ze to jest referencja meh
            }

            countdownEvent.Wait();
        }
        ///*
        // * int height = (int) ((object[]) dane)[0];
        //   int width = (int) ((object[]) dane)[1];
        //   float[][] values = (float[]) ((object[]) dane)[2];// część
        //   int iteracje = (int) ((object[]) dane)[3];
        //   int[] extraInfo = (int[])((object[])dane)[4]; // ile razy nie usuwac z gory, (mid, top, bottom), ile razy nie usuwac z dolu
        //   CountdownEvent c = (CountdownEvent) ((object[]) dane)[5];
        //    int ktory = (int)((object[])dane)[6];
        // */
        ///////////////////////////////////////////////////



        #endregion

        #region syncFor

        public static void insidePAR(object ob)
        {
            float[][] tIN = (float[][])((object[])ob)[0];
            int height = (int)((object[])ob)[1];
            int width = (int)((object[])ob)[2];
            float[][] tOUT = (float[][])((object[])ob)[3];
            Parallel.For(1, height, i =>
            {
                Parallel.For(1, width, j =>
                {
                    float pixel = tIN[i][j] * 0.6f;
                    pixel += tIN[i - 1][j] * 0.1f;
                    pixel += tIN[i + 1][j] * 0.1f;
                    pixel += tIN[i][j - 1] * 0.1f;
                    pixel += tIN[i][j + 1] * 0.1f;
                    tOUT[i][j] = pixel;
                });
            });

        }

        public static void outsidePAR(object ob)
        {
            float[][] tIN = (float[][])((object[])ob)[0];
            int height = (int)((object[])ob)[1];
            int width = (int)((object[])ob)[2];
            float[][] tOUT = (float[][])((object[])ob)[3];

            Parallel.For(1, width - 1, i =>
            { // ^- pierwszy wiersz pikseli obrazka
                tOUT[0][i] =
                    tIN[0][i] * 0.6f +
                    tIN[0][i + 1] * 0.1f +
                    tIN[0][i - 1] * 0.1f +
                    tIN[1][i] * 0.1f;
                // v- ostatni wiersz pikseli obrazka
                tOUT[height - 1][i] =
                    tIN[height - 1][i] * 0.6f +
                     tIN[height - 1][i + 1] * 0.1f +
                     tIN[height - 1][i - 1] * 0.1f +
                     tIN[height - 2][i] * 0.1f;
            });
            for (int i = 1; i < height - 1; i++)
            { // <| pierwsza kolumna pikseli obrazka
                tOUT[i][0] =
                    tIN[i][0] * 0.6f +
                    tIN[i][1] * 0.1f +
                    tIN[i + 1][0] * 0.1f +
                    tIN[i - 1][0] * 0.1f;
                // >| ostatnia kolumna pikseli obrazka
                tOUT[i][width - 1] =
                    tIN[i][width - 1] * 0.6f +
                    tIN[i][width - 2] * 0.1f +
                    tIN[i + 1][width - 1] * 0.1f +
                    tIN[i - 1][width - 1] * 0.1f;
            }
            //piksel w lewym górnym rogu obrazka
            tOUT[0][0] =
                    tIN[0][0] * 0.6f +
                    //piksel na prawo
                    tIN[0][1] * 0.1f +
                    //piksel pod nim
                    tIN[1][0] * 0.1f;
            //piksel w prawym górnym rogu
            tOUT[0][width - 1] =
                    tIN[0][width - 1] * 0.6f +
                    //piksel na lewo
                    tIN[0][width - 2] * 0.1f +
                    //piksel pod nim
                    tIN[1][width - 1] * 0.1f;
            //piksel w lewym dolnym rogu
            tOUT[height - 1][0] =
                    tIN[height - 1][0] * 0.6f +
                    //piksel nad nim
                    tIN[height - 2][0] * 0.1f +
                    //piksel na prawo
                    tIN[height - 1][1] * 0.1f;
            //piksel w prawym dolnym rogu
            tOUT[height - 1][width - 1] =
                    tIN[height - 1][width - 1] * 0.6f +
                    //piksel na lewo
                    tIN[height - 1][width - 2] * 0.1f +
                    //piksel nad nim
                    tIN[height - 2][width - 1] * 0.1f;
        }


        public void syncFor(int przejscia, int height, int width)
        {

            var t2 = inicjalizujTablice(height, width);
            int ile = 0;
            while (ile < przejscia)
            {
                insidePAR(new object[] { Wartosci, height - 1, width - 1, t2 });
                outsidePAR(new object[] { Wartosci, height, width, t2 });
                ile++;
                insidePAR(new object[] { t2, height - 1, width - 1, Wartosci });
                outsidePAR(new object[] { t2, height, width, Wartosci });
                ile++;
            }




        }


        #endregion


        #endregion
    }
}