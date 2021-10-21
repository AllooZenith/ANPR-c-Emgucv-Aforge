using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AForge;
using AForge.Math.Geometry;
using AForge.Imaging;
using AForge.Video;
using AForge.Imaging.Filters;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.OCR;
using AForge.Video.DirectShow;
using MSER_c;
using AForge.Math.Metrics;
//=============================================================//
//
//                      Project Info
//
//=============================================================//
//
//                      Project Supervisor 
//
//                      1. Dr.Medhi Hasan
//
//=============================================================//
//
//                          ANPR
//
//=============================================================//
//
//                      Group Members
//      1. Muhammad Ibrahim         120665
//      2. Muhammad Arham Shahzad   120623
//      3. ZaheerUdin Babar         120645 
//
//=============================================================//
//
//
//      .Help Taken by Irtiza Hasan in understanding of Mser (Thersholding)
//
//
//=============================================================//
//
//                  Libraies Used
//      1.Aforge (Latest Version) => for filters
//      2.Emguc  (latest Version) => for OCr 
//
//=============================================================//

namespace anprTry1
{
    public partial class Form1 : Form
    {
        Bitmap image, currFrame; // holds the image 
        Dictionary<Double, int> dict = new Dictionary<Double, int>();  // dictonary used to know the occurance of certian blobs
        List<cog1> Area = new List<cog1>(); // class object for storing blobs information
        private Bitmap videoFrame;  // used for holding frame of a video
        OpenFileDialog op = new OpenFileDialog(); // open picture
        BlobCounterBase bcb = new BlobCounter();  //  searching blob to process by using connected components 
        Double check = 15; // for skipping first 15 frames of video (60fps)
        Bitmap autoCropImage; // holds original image for mapping cordinates of crop image from low reselotion 
        int k;
        Bitmap original;


        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
             
                pictureBox2.Image = null;
                pictureBox3.Image = null;
                pictureBox1.Image = null;
                pictureBox4.Image = null;
                pictureBox5.Image = null;
                textBox1.Text = null;
                textBox2.Text = null;
                OpenFileDialog open = new OpenFileDialog();

                open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp;*.tif)|*.jpg; *.jpeg; *.gif; *.bmp;*.tif";

                if (open.ShowDialog() == DialogResult.OK)
                {
                    original = null;
                    image = new Bitmap(open.FileName);

                    original = image;
                    Bitmap pyramid;
                    int width, hight;

                    pyramid = image;
                    // Resizing of an image and storing original
                    //k = 3;
                    k = image.Width / 768;
                    width = image.Width / k;
                    hight = image.Height / k;
                    // create filter image pyari mid  ResizeBicubic ResizeNearestNeighbor
                    ResizeBilinear filter = new ResizeBilinear(width, hight);
                    // apply the filter
                    Bitmap newImage = filter.Apply(pyramid);
                    image = newImage;
                    pictureBox1.Image = original;


                }
            }
            catch (Exception)
            {
                throw new ApplicationException("Failed loading image");
            }

        }

        private void greyScaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {



                //-----------------------------------------------------------//
                //
                //
                // ============= Detection of rectangle =====================//
                //
                //
                //----------MSER (MAximum Stable External Regions)-----------//


                // grayscaling of an Image
                Bitmap IndexedImage = new Bitmap(image);
                Grayscale filter1 = new Grayscale(0.2125, 0.7154, 0.0721);
                IndexedImage = filter1.Apply(IndexedImage);

       
                Bitmap bitmap = new Bitmap(IndexedImage);
                int width = bitmap.Width;
                int height = bitmap.Height;
                int lopw = bitmap.Width / 6;
                int loph = bitmap.Height / 6; // dividing image by for fastprocessing 
                //===============================//
                //       MSER starting
                //===============================//
                BlobCounterBase bc = new BlobCounter();
                Blob[] blobs = null;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                for (int t = 80; t <= 220; t++)
                {
                    
                    //locking
                    bitmap = new Bitmap(IndexedImage);
                    FastBitmap process = new FastBitmap(bitmap);
                    process.LockImage();
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height-loph; j++) // loop will skip lower 1/6 of an image 
                        {
                            if (process.GetPixel(i, j).R > t)               // THIS IS THE LOCKED IMAGE PROCESSING BIT
                                process.SetPixel(i, j, Color.White);
                            else
                                process.SetPixel(i, j, Color.Black);
                        }

                    }


                    process.UnlockImage();//unlock
                                          //playing with blob
                    currFrame =  new Bitmap(bitmap);

                   // setting blob properties to avoid unwanted blobs
                    bc.FilterBlobs = true;
                    bc.MinHeight = 17;
                    bc.MinWidth = 30;
                    if (checkBox1.Checked) 
                    {
                        bc.MinHeight = 10;
                        bc.MinWidth = 10;
                    }
                    else
                    {
                        bc.MaxHeight = 80;
                        bc.MaxWidth = 120;
                    }
                    bc.ProcessImage(currFrame); // working on image for blob extraction
                    blobs = bc.GetObjectsInformation();
                    Graphics g = Graphics.FromImage(currFrame);  
                    SimpleShapeChecker shapeChecker = new SimpleShapeChecker(); 

                    for (int i = 0, n = blobs.Length; i < n; i++)
                    {
                        // getting data of blob
                        List<IntPoint> edgePoints = bc.GetBlobsEdgePoints(blobs[i]); 
                        List<IntPoint> corners;

                        if (shapeChecker.IsQuadrilateral(edgePoints, out corners))
                            if (shapeChecker.CheckPolygonSubType(corners) == PolygonSubType.Rectangle)// checking shape if it is rectangle 
                            {
                                
                                bc.ExtractBlobsImage(image, blobs[i], false);
                             
                                cog1 contanior = new cog1();
                                // saving blob's specific elements which are needed for further processing
                                contanior.widthex = blobs[i].Rectangle.Width;
                                contanior.Hightex = blobs[i].Rectangle.Height;
                                contanior.pointx = blobs[i].Rectangle.X;
                                contanior.pointy = blobs[i].Rectangle.Y;
                                contanior.area = blobs[i].Rectangle.Height * blobs[i].Rectangle.Width;
                                Area.Add(contanior); // addaing blob to list for further processing

                            }        
                    }

                }
                //===============================//
                //       MSER Ending 
                //===============================//

                // adding data to dictonary so that it can tell us the occurance of Blobs area 
                if (Area.Count>0)
                {
                    foreach (cog1 word in Area)
                    {
                        if (dict.ContainsKey(word.area))
                        {
                            dict[word.area]++;
                        }
                        else
                        {
                            dict[word.area] = 1;
                        }
                    }

                    // ordring of key value pairs by decending so that most occured area comes on top
                    var newDictionary = dict.Where(pair => pair.Value >= 1)
                                                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                    newDictionary = newDictionary.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                  
                    int flag = 0;
       
                    // creating a list of rectangles to store wanted rectangles which qualify to be numberplates
                    List<Rectangle> rec1 = new List<Rectangle>();

                    Bitmap kc = new Bitmap(pictureBox1.Image);
                    // using key & values for loop to find the matching blob with that specific occured area 
                    foreach (KeyValuePair<Double, int> entry in newDictionary)
                    {
                        Double temp = entry.Key;
                        foreach (cog1 c in Area)  // loop of stored blobls and matching with area 
                        {
                            if (flag == 1) // if one area is found delete that value so that outer loops only runs to newDictonary.count+1
                            {
                                newDictionary.Remove(entry.Value);
                            }
                            // filtring of wanted blob
                            // it should have a width to hight ratio of 1.33 to 4.26 and blob starting point should not be equal to xand y =0 
                            if (c.area == temp && c.widthex / c.Hightex > 1.33 && c.widthex / c.Hightex <= 4.26 && c.pointx != 0 && c.pointy != 0)
                            {
                                //adding the rectangle to list for further processing and to draw boundry box
                                Rectangle r1 = new Rectangle(c.pointx, c.pointy, Convert.ToInt32(c.widthex), Convert.ToInt32(c.Hightex));

                                //adding of rectangle to list 
                                rec1.Add(r1);
                                flag = 0;
                                goto end;

                            }
                            // if fails that condition remove that area 
                            else { flag = 1; }
                                    
                        }
                        end:;                        
                    }

                    int ck = 0; //  to check if there is only one numberplate
                    // removes duplicates from the list of rect1
                    var noDupes = rec1.Distinct().ToList();
                    // ordering the list by Xcordinate of rectangle 
                    var storelist = noDupes.OrderBy(o => o.X).ToList();
                    // storing the sorted list and also creating new 
                    List<Rectangle> SortedList = noDupes.OrderBy(o => o.X).ToList();
                    List<Rectangle> semifinalrec = new List<Rectangle>();
                    //will mostly remove the overlaping rectangles except the back to back overlaping e.g  r1,r2,r2,r1  will not be removed 
                    for (int l = 0; l < SortedList.Count - 1; l++)
                    {
                        Rectangle rectangle1 = new Rectangle(SortedList.ElementAt(l).X, SortedList.ElementAt(l).Y, SortedList.ElementAt(l).Width, SortedList.ElementAt(l).Height);
                        Rectangle rectangle2 = new Rectangle(SortedList.ElementAt(l + 1).X, SortedList.ElementAt(l + 1).Y, SortedList.ElementAt(l + 1).Width, SortedList.ElementAt(l + 1).Height);
                        if (Rectangle.Intersect(rectangle2, rectangle1) != Rectangle.Empty) ;
                        else
                        {
                            semifinalrec.Add(rectangle1);
                            semifinalrec.Add(rectangle2);
                            ck = 1; //  to check if there is only one numberplate
                        }
                    }

                    // will also remove the back to back overlaping rectangles and will give the list of rectangles with no overlaping
                    List<Rectangle> semifinalrec2 = new List<Rectangle>();
                    for (int l = 0; l < semifinalrec.Count - 1; l++)
                    {
                        Rectangle rectangle1 = new Rectangle(semifinalrec.ElementAt(l).X, semifinalrec.ElementAt(l).Y, semifinalrec.ElementAt(l).Width, semifinalrec.ElementAt(l).Height);
                        Rectangle rectangle2 = new Rectangle(semifinalrec.ElementAt(l + 1).X, semifinalrec.ElementAt(l + 1).Y, semifinalrec.ElementAt(l + 1).Width, semifinalrec.ElementAt(l + 1).Height);
                        if (Rectangle.Intersect(rectangle2, rectangle1) != Rectangle.Empty) ;
                        else
                        {
                            if (l == 0)
                            {
                                semifinalrec2.Add(rectangle1);
                            }
                            semifinalrec2.Add(rectangle2);
                            ck = 1;
                        }
                    }
                    // to check if there is no numberplate if there is not processing will be stoped 

                    if (noDupes.Count != 0)
                    {
                        
                        Pen redPen = new Pen(Color.Red, 10); //  to draw rectangles 

                        noDupes = semifinalrec2.Distinct().ToList();
                       // drawing of numberplates 
                        for (int h = 0; h < noDupes.Count; h++)
                        {
                            if (h != noDupes.Count)
                            {
                             // will crop and draw numberplate if more then one 1st numberplate in imageBox1 

                                if (h == 0)
                                { 
                                    // croping of image from original image 
                                    Crop cropfilter = new Crop(new Rectangle((noDupes.ElementAt(h).X * k) - 10, (noDupes.ElementAt(h).Y * k) - 10, ((noDupes.ElementAt(h).Width * k) + 20), (noDupes.ElementAt(h).Height * k) + 20));
                                    // apply the filter
                                    autoCropImage = cropfilter.Apply(original);
                                    pictureBox2.Image = autoCropImage;

                                    //  using of ocr on croped image 
                                    var resultImage = new Bitmap(pictureBox2.Image);
                                    Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage); // converting of system.drawimg.image to emgucv image array
                                    Emgu.CV.OCR.Tesseract _ocr;
                                    //applying ocr
                                    StringBuilder strBuilder = new StringBuilder();
                                    _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                                    _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                                    _ocr.Recognize(myImage);
                                    Emgu.CV.OCR.Tesseract.Character[] words;
                                    // getting of text 
                                    words = _ocr.GetCharacters();
                                    for (int i = 0; i < words.Length; i++)
                                    {
                                        strBuilder.Append(words[i].Text);
                                    }
                                    string str;
                                    str = Convert.ToString(strBuilder.ToString());
                                    if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ; // checking if string is present or is empty then dont add
                                    else
                                    { listBox1.Items.Add(str); }




                                }

                                //  for numberplate two same process is done
                                if (h == 1)
                                {
                                    Crop cropfilter = new Crop(new Rectangle((noDupes.ElementAt(h).X * k) - 10, (noDupes.ElementAt(h).Y * k) - 10, ((noDupes.ElementAt(h).Width * k) + 20), (noDupes.ElementAt(h).Height * k) + 20));
                                    // apply the filter
                                    autoCropImage = cropfilter.Apply(original);
                                    pictureBox3.Image = autoCropImage;

                                    var resultImage = new Bitmap(pictureBox3.Image);
                                    Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage);
                                    Emgu.CV.OCR.Tesseract _ocr;

                                    StringBuilder strBuilder = new StringBuilder();
                                    _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                                    _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                                    _ocr.Recognize(myImage);
                                    Emgu.CV.OCR.Tesseract.Character[] words;
                                    words = _ocr.GetCharacters();
                                    for (int i = 0; i < words.Length; i++)
                                    {
                                        strBuilder.Append(words[i].Text);
                                    }
                                    string str;
                                    str = Convert.ToString(strBuilder.ToString());
                                    if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ;
                                    else
                                    { listBox1.Items.Add(str); }
                                }
                                // for numberplate 3
                                if (h == 2)
                                {
                                    Crop cropfilter = new Crop(new Rectangle((noDupes.ElementAt(h).X * k) - 10, (noDupes.ElementAt(h).Y * k) - 10, ((noDupes.ElementAt(h).Width * k) + 20), (noDupes.ElementAt(h).Height * k) + 20));
                                    // apply the filter
                                    autoCropImage = cropfilter.Apply(original);
                                    pictureBox4.Image = autoCropImage;

                                    var resultImage = new Bitmap(pictureBox4.Image);
                                    Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage);
                                    Emgu.CV.OCR.Tesseract _ocr;

                                    StringBuilder strBuilder = new StringBuilder();
                                    _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                                    _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                                    _ocr.Recognize(myImage);
                                    Emgu.CV.OCR.Tesseract.Character[] words;
                                    words = _ocr.GetCharacters();
                                    for (int i = 0; i < words.Length; i++)
                                    {
                                        strBuilder.Append(words[i].Text);
                                    }
                                    string str;
                                    str = Convert.ToString(strBuilder.ToString());
                                    if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ;
                                    else
                                    { listBox1.Items.Add(str); }
                                }
                                 // for numberplate 4 
                                if (h == 3)
                                {
                                    Crop cropfilter = new Crop(new Rectangle((noDupes.ElementAt(h).X * k) - 10, (noDupes.ElementAt(h).Y * k) - 10, ((noDupes.ElementAt(h).Width * k) + 20), (noDupes.ElementAt(h).Height * k) + 20));
                                    // apply the filter
                                    autoCropImage = cropfilter.Apply(original);
                                    pictureBox5.Image = autoCropImage;

                                    var resultImage = new Bitmap(pictureBox5.Image);
                                    Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage);
                                    Emgu.CV.OCR.Tesseract _ocr;

                                    StringBuilder strBuilder = new StringBuilder();
                                    _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                                    _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                                    _ocr.Recognize(myImage);
                                    Emgu.CV.OCR.Tesseract.Character[] words;
                                    words = _ocr.GetCharacters();
                                    for (int i = 0; i < words.Length; i++)
                                    {
                                        strBuilder.Append(words[i].Text);
                                    }
                                    string str;
                                    str = Convert.ToString(strBuilder.ToString());
                                    if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ;
                                    else
                                    { listBox1.Items.Add(str); }
                                    break;
                                }
                                // drawing of an all the rectangles over numberplates on original image 
                                Graphics g = Graphics.FromImage(kc);
                                g.DrawRectangle(redPen, (noDupes.ElementAt(h).X * k) - 10, (noDupes.ElementAt(h).Y * k) - 10, ((noDupes.ElementAt(h).Width * k) + 20), (noDupes.ElementAt(h).Height * k) + 20);
                                pictureBox1.Image = kc;
                                g.Dispose();
                                textBox1.Text = Convert.ToString(h + 1); // shown the total number plates detected 
                            }


                        }

                        // if there was only one numberplate ck=0 then this code will run with same above logic but only at storelist index 0 
                        if (ck != 1)
                        {
                            // drawing of an rectangle and cropping and using of an ocr with same logic 
                            Graphics g = Graphics.FromImage(kc);
                            g.DrawRectangle(redPen, new Rectangle(storelist.ElementAt(0).X * k, storelist.ElementAt(0).Y * k, storelist.ElementAt(0).Width * k, storelist.ElementAt(0).Height * k));
                            pictureBox1.Image = kc;
                            g.Dispose();
                             // croping of an image 
                            Crop cropfilter = new Crop(new Rectangle((storelist.ElementAt(0).X * k) - 10, (storelist.ElementAt(0).Y * k) - 10, ((storelist.ElementAt(0).Width * k) + 20), (storelist.ElementAt(0).Height * k) + 20));
                            // apply the filter
                            autoCropImage = cropfilter.Apply(original);
                            pictureBox2.Image = autoCropImage;
                            // using ocr 
                            var resultImage = new Bitmap(pictureBox2.Image);
                            Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage);
                            Emgu.CV.OCR.Tesseract _ocr;

                            StringBuilder strBuilder = new StringBuilder();
                            _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                            _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                            _ocr.Recognize(myImage);
                            Emgu.CV.OCR.Tesseract.Character[] words;
                            words = _ocr.GetCharacters();
                            for (int i = 0; i < words.Length; i++)
                            {
                                strBuilder.Append(words[i].Text);
                            }
                            string str;
                            str = Convert.ToString(strBuilder.ToString());
                            if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ;
                            else
                            { listBox1.Items.Add(str); }
                        }


                        if (pictureBox2.Image == null)
                        {

                            pictureBox2.Image = null;
                        }

                        if (pictureBox3.Image == null)
                        {

                            pictureBox3.Image = null;
                        }

                        if (pictureBox4.Image == null)
                        {
                            pictureBox4.Image = null;
                            
                        }

                        if (pictureBox5.Image == null)
                        {
                            pictureBox5.Image = null;
                        }
                        redPen.Dispose();
                    }
                    else
                    {
                        MessageBox.Show("No successful candidate for number plate");
                    }
                    // removing all the valuse from ist and other objects so that for next itration there is no garbage value
                    newDictionary.Clear();
                    Area.Clear();
                    bc = null;
                    bcb = null;
                    noDupes = null;
                    SortedList = null;
                    semifinalrec = null;
                    rec1 = null;
                    semifinalrec2 = null;
                    
                }

           
                // time shown  which is taken to prcess image 
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                
                textBox2.Text=Convert.ToString(TimeSpan.FromMilliseconds(elapsedMs).TotalSeconds);
            }


            catch (Exception ex)
            {
                MessageBox.Show("" + ex);
            }
        }


        // ===============================================================================//


        //------------------------- Video Processing Starting----------------------------//


        //===============================================================================//


        private void videoToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {


        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (check == 20 ) // first five frames will be skipped then onwards 20 framse will be skipped 
            {

                pictureBox2.Image = null;
                pictureBox3.Image = null;
                pictureBox4.Image = null;
                pictureBox5.Image = null;


                check = 0;

                // get new frame
                Bitmap bitmap = eventArgs.Frame;
                // process the frame and store frame for mapping 
                videoFrame = bitmap;
                pictureBox1.Image = (Bitmap)videoFrame.Clone();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Bitmap   pyramid, currFrame;
                Bitmap imagesimple = videoFrame;
                int  hight, widthh, div;
                div = 4; //  resize of frame 
                
                pyramid = imagesimple;
                widthh = imagesimple.Width / div;
                hight = imagesimple.Height / div;

                ResizeBilinear filter = new ResizeBilinear(widthh, hight);
                // apply the filter
                Bitmap newImage = filter.Apply(pyramid);

                // grayscale conversion
                Bitmap IndexedImage = new Bitmap(newImage);
                Grayscale filter1 = new Grayscale(0.2125, 0.7154, 0.0721);
                IndexedImage = filter1.Apply(IndexedImage);
                

                 pyramid = new Bitmap(IndexedImage);
                int width = pyramid.Width;
                int height = pyramid.Height;
                //===========================================//
                //
                //--------------MSER starting----------------//
                //
                //===========================================//
                BlobCounterBase bc = new BlobCounter();
                Blob[] blobs = null;


                for (int t = 80; t <= 220; t++)
                {
                    //locking
                    bitmap = new Bitmap(IndexedImage);
                    FastBitmap process = new FastBitmap(bitmap);
                    process.LockImage();
                    for (int i = 0; i < width ; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            if (process.GetPixel(i, j).R > t)               // THIS IS THE LOCKED IMAGE PROCESSING BIT
                                process.SetPixel(i, j, Color.White);
                            else
                                process.SetPixel(i, j, Color.Black);
                        }

                    }


                    process.UnlockImage();//unlock
                                          //playing with blob
                    currFrame = new Bitmap(bitmap);

                    //Aspect Ratio Filtering
                    bc.FilterBlobs = true; 
                    bc.MinHeight = 17;
                    bc.MinWidth = 30;
                    if (checkBox1.Checked)
                    {
                        bc.MinHeight = 10;
                        bc.MinWidth = 10;
                    }
                    else
                    {
                        bc.MaxHeight = 80;
                        bc.MaxWidth = 120;
                    }
                        // blob extraction
                    bc.ProcessImage(currFrame);
                    blobs = bc.GetObjectsInformation();
                    Graphics g = Graphics.FromImage(currFrame);
                   
                    SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

                    for (int i = 0, n = blobs.Length; i < n; i++)
                    {
                        // storing of edgepints cordinates of image
                        List<IntPoint> edgePoints = bc.GetBlobsEdgePoints(blobs[i]);
                        List<IntPoint> corners;

                        if (shapeChecker.IsQuadrilateral(edgePoints, out corners))
                            if (shapeChecker.CheckPolygonSubType(corners) == PolygonSubType.Rectangle) // shape checking if its rectangle 
                            {
                                // blob elemets saing to list which will be neede later 
                                bc.ExtractBlobsImage(pyramid, blobs[i], false);
                                cog1 contanior = new cog1();
                                contanior.widthex = blobs[i].Rectangle.Width;
                                contanior.Hightex = blobs[i].Rectangle.Height;
                                contanior.pointx = blobs[i].Rectangle.X;
                                contanior.pointy = blobs[i].Rectangle.Y;
                                contanior.area = blobs[i].Rectangle.Height * blobs[i].Rectangle.Width; // calculating area 
                                Area.Add(contanior); // add to list 

                            }
                                  
                    }
                 
                } 
                //========================//
                //-----Mser Ending--------//
                //========================//


                // addaing area to dictonary to find most repeted area (stable area)
                foreach (cog1 word in Area)
                {
                    if (dict.ContainsKey(word.area))
                    {
                        dict[word.area]++;
                    }
                    else
                    {
                        dict[word.area] = 1;
                    }
                }
                // sorting it by decending order 
                var newDictionary = dict.Where(pair => pair.Value >= 1)
                                                .ToDictionary(pair => pair.Key, pair => pair.Value);
                newDictionary = newDictionary.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                int flag = 0;

                Bitmap kc = new Bitmap(videoFrame);  // kc original image 

                // rectangle list 
                 
                List<Rectangle> rec1 = new List<Rectangle>();

                foreach (KeyValuePair<Double, int> entry in newDictionary)
                {
                    Double temp = entry.Key;
                    foreach (cog1 c in Area)  // matching area from divtonary to Area.area 
                    {
                        if (flag == 1) // rmove if no area qualify / blob
                        {
                            newDictionary.Remove(entry.Value);
                        } 
                        // blob filtring for it to be numberplate 
                        if (c.area == temp && c.widthex / c.Hightex > 1.1 && c.widthex / c.Hightex <= 4.26 && c.pointx != 0 && c.pointy != 0)
                        {
                            Rectangle r1 = new Rectangle(c.pointx, c.pointy, Convert.ToInt32(c.widthex), Convert.ToInt32(c.Hightex));
                            // add that blob to list 
                            rec1.Add(r1);

                            flag = 0;
                            goto end;
                        }
                        else { flag = 1; }

                    }
                    end:;

                }

                int ck = 0; 
                // removing duplicates 
                var noDupes = rec1.Distinct().ToList();
                var storelist = noDupes.OrderBy(o => o.X).ToList(); // ordering by X cordinate 
                // creating list for remobing overlaping  of rectangles 
                List<Rectangle> SortedList = noDupes.OrderBy(o => o.X).ToList();
                List<Rectangle> semifinalrec = new List<Rectangle>();
                var noDupes2 = noDupes.ToList();
                // will remove the overlaping but back to back will remain 
                for (int l = 0; l < SortedList.Count - 1; l++)
                {
                    Rectangle rectangle1 = new Rectangle(SortedList.ElementAt(l).X, SortedList.ElementAt(l).Y, SortedList.ElementAt(l).Width, SortedList.ElementAt(l).Height);
                    Rectangle rectangle2 = new Rectangle(SortedList.ElementAt(l + 1).X, SortedList.ElementAt(l + 1).Y, SortedList.ElementAt(l + 1).Width, SortedList.ElementAt(l + 1).Height);
                    if (Rectangle.Intersect(rectangle2, rectangle1) != Rectangle.Empty) ;
                    else
                    {
                        semifinalrec.Add(rectangle1);
                        semifinalrec.Add(rectangle2);
                        ck = 1;
                    }
                }
                // will remove back to back overlaping 
                List<Rectangle> semifinalrec2 = new List<Rectangle>();
                for (int l = 0; l < semifinalrec.Count - 1; l++)
                {
                    Rectangle rectangle1 = new Rectangle(semifinalrec.ElementAt(l).X, semifinalrec.ElementAt(l).Y, semifinalrec.ElementAt(l).Width, semifinalrec.ElementAt(l).Height);
                    Rectangle rectangle2 = new Rectangle(semifinalrec.ElementAt(l + 1).X, semifinalrec.ElementAt(l + 1).Y, semifinalrec.ElementAt(l + 1).Width, semifinalrec.ElementAt(l + 1).Height);
                    if (Rectangle.Intersect(rectangle2, rectangle1) != Rectangle.Empty) ;
                    else
                    {
                        if (l == 0)
                        {
                            semifinalrec2.Add(rectangle1);
                        }
                        semifinalrec2.Add(rectangle2);
                        ck = 1;
                    }
                }


                Pen redPen = new Pen(Color.Red, 10);
               
                noDupes = semifinalrec2.Distinct().ToList();
                if (ck == 1)
                {
                    for (int h = 0; h < noDupes.Count; h++)
                    {
                        
                        // if there are more then one numberplates 

                        if (h == 0) // numberplate 1 in imagebox1
                        {
                            // draw rectangle around numberplate 

                            Graphics g = Graphics.FromImage(kc);
                            g.DrawRectangle(redPen, (noDupes.ElementAt(h).X * div) - 10, (noDupes.ElementAt(h).Y * div) - 10, ((noDupes.ElementAt(h).Width * div) + 20), (noDupes.ElementAt(h).Height * div) + 20);
                            pictureBox1.Image = kc;
                            g.Dispose();

                            // crop numberplate and map it to original 
                            Crop cropfilter = new Crop(new Rectangle((noDupes.ElementAt(h).X * div) - 10, (noDupes.ElementAt(h).Y * div) - 10, ((noDupes.ElementAt(h).Width * div) + 20), (noDupes.ElementAt(h).Height * div) + 20));
                            // apply the filter

                            autoCropImage = cropfilter.Apply(imagesimple);

                            pictureBox2.Image = autoCropImage;

                            // applying of ocr using emgucv

                            var resultImage = new Bitmap(pictureBox2.Image);

                            Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage);
                            Emgu.CV.OCR.Tesseract _ocr;

                            StringBuilder strBuilder = new StringBuilder();
                            _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                            _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                            _ocr.Recognize(myImage);
                            Emgu.CV.OCR.Tesseract.Character[] words;
                            words = _ocr.GetCharacters();
                            for (int i = 0; i < words.Length; i++)
                            {
                                strBuilder.Append(words[i].Text);
                            }
                            string str;
                            str = Convert.ToString(strBuilder.ToString());
                            if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ;
                            else
                            {
                                this.Invoke(new MethodInvoker(delegate () { listBox1.Items.Add(str); })); // if thatstring already exist or is empty dont add
                            }

                            _ocr.Dispose();


                        }

                        if (h == 1) // for numberplate 2
                        {
                            Graphics g = Graphics.FromImage(kc);
                            g.DrawRectangle(redPen, (noDupes.ElementAt(h).X * div) - 10, (noDupes.ElementAt(h).Y * div) - 10, ((noDupes.ElementAt(h).Width * div) + 20), (noDupes.ElementAt(h).Height * div) + 20);
                            pictureBox1.Image = kc;
                            g.Dispose();
                            Crop cropfilter = new Crop(new Rectangle((noDupes.ElementAt(h).X * div) - 10, (noDupes.ElementAt(h).Y * div) - 10, ((noDupes.ElementAt(h).Width * div) + 20), (noDupes.ElementAt(h).Height * div) + 20));
                            // apply the filter
                            autoCropImage = cropfilter.Apply(imagesimple);
                            pictureBox3.Image = autoCropImage;

                            var resultImage = new Bitmap(pictureBox3.Image);
                            Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage);
                            Emgu.CV.OCR.Tesseract _ocr;

                            StringBuilder strBuilder = new StringBuilder();
                            _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                            _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                            _ocr.Recognize(myImage);
                            Emgu.CV.OCR.Tesseract.Character[] words;
                            words = _ocr.GetCharacters();
                            for (int i = 0; i < words.Length; i++)
                            {
                                strBuilder.Append(words[i].Text);
                            }
                            string str;
                            str = Convert.ToString(strBuilder.ToString());
                            if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ;
                            else
                            {
                                this.Invoke(new MethodInvoker(delegate () { listBox1.Items.Add(str); }));
                            }
                            _ocr.Dispose();
                        }

                        if (h == 2) // for numberplate 3
                        {
                            Graphics g = Graphics.FromImage(kc);
                            g.DrawRectangle(redPen, (noDupes.ElementAt(h).X * div) - 10, (noDupes.ElementAt(h).Y * div) - 10, ((noDupes.ElementAt(h).Width * div) + 20), (noDupes.ElementAt(h).Height * div) + 20);
                            pictureBox1.Image = kc;
                            g.Dispose();
                            Crop cropfilter = new Crop(new Rectangle((noDupes.ElementAt(h).X * div) - 10, (noDupes.ElementAt(h).Y * div) - 10, ((noDupes.ElementAt(h).Width * div) + 20), (noDupes.ElementAt(h).Height * div) + 20));
                            // apply the filter
                            autoCropImage = cropfilter.Apply(imagesimple);
                            pictureBox4.Image = autoCropImage;

                            var resultImage = new Bitmap(pictureBox4.Image);
                            Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage);
                            Emgu.CV.OCR.Tesseract _ocr;

                            StringBuilder strBuilder = new StringBuilder();
                            _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                            _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                            _ocr.Recognize(myImage);
                            Emgu.CV.OCR.Tesseract.Character[] words;
                            words = _ocr.GetCharacters();
                            for (int i = 0; i < words.Length; i++)
                            {
                                strBuilder.Append(words[i].Text);
                            }
                            string str;
                            str = Convert.ToString(strBuilder.ToString());
                            if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ;
                            else
                            {
                                this.Invoke(new MethodInvoker(delegate () { listBox1.Items.Add(str); }));
                            }
                            _ocr.Dispose();
                        }

                        if (h == 3) // for numberplate 4 
                        {
                            Graphics g = Graphics.FromImage(kc);
                            g.DrawRectangle(redPen, (noDupes.ElementAt(h).X * div) - 10, (noDupes.ElementAt(h).Y * div) - 10, ((noDupes.ElementAt(h).Width * div) + 20), (noDupes.ElementAt(h).Height * div) + 20);
                            pictureBox1.Image = kc;
                            g.Dispose();
                            Crop cropfilter = new Crop(new Rectangle((noDupes.ElementAt(h).X * div) - 10, (noDupes.ElementAt(h).Y * div) - 10, ((noDupes.ElementAt(h).Width * div) + 20), (noDupes.ElementAt(h).Height * div) + 20));
                            // apply the filter
                            autoCropImage = cropfilter.Apply(imagesimple);
                            pictureBox5.Image = autoCropImage;

                            var resultImage = new Bitmap(pictureBox5.Image);
                            Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage);
                            Emgu.CV.OCR.Tesseract _ocr;

                            StringBuilder strBuilder = new StringBuilder();
                            _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                            _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                            _ocr.Recognize(myImage);
                            Emgu.CV.OCR.Tesseract.Character[] words;
                            words = _ocr.GetCharacters();
                            for (int i = 0; i < words.Length; i++)
                            {
                                strBuilder.Append(words[i].Text);
                            }
                            string str;
                            str = Convert.ToString(strBuilder.ToString());
                            if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ;
                            else
                            {
                                this.Invoke(new MethodInvoker(delegate () { listBox1.Items.Add(str); }));
                            }
                            _ocr.Dispose();
                        }

                    }
                }
                else //  will  be for only one numberplate 
                {
                    // drawing of rectangle 

                    Graphics g = Graphics.FromImage(kc);
                    g.DrawRectangle(redPen, (noDupes2.ElementAt(0).X * div) - 10, (noDupes2.ElementAt(0).Y * div) - 10, ((noDupes2.ElementAt(0).Width * div) + 20), (noDupes2.ElementAt(0).Height * div) + 20);
                    pictureBox1.Image = kc;
                    g.Dispose();
                    pictureBox1.Image = kc;
                    Crop cropfilter = new Crop(new Rectangle((storelist.ElementAt(0).X * div) - 10, (storelist.ElementAt(0).Y * div) - 10, ((storelist.ElementAt(0).Width * div) + 20), (storelist.ElementAt(0).Height * div) + 20));
                    // apply the filter
                    autoCropImage = cropfilter.Apply(imagesimple);
                    pictureBox2.Image = autoCropImage;

                    var resultImage = new Bitmap(pictureBox2.Image);
                    Emgu.CV.Image<Bgr, Byte> myImage = new Image<Bgr, Byte>(resultImage);
                    Emgu.CV.OCR.Tesseract _ocr;

                    StringBuilder strBuilder = new StringBuilder();
                    _ocr = new Emgu.CV.OCR.Tesseract(@"C:\Emgu\emgucv-windows-universal 3.0.0.2157\bin\tessdata", "eng", OcrEngineMode.Default);
                    _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890");
                    _ocr.Recognize(myImage);
                    Emgu.CV.OCR.Tesseract.Character[] words;
                    words = _ocr.GetCharacters();
                    for (int i = 0; i < words.Length; i++)
                    {
                        strBuilder.Append(words[i].Text);
                    }
                    string str;
                    str = Convert.ToString(strBuilder.ToString());
                    if (listBox1.Items.Contains(str) || string.IsNullOrWhiteSpace(str)) ;
                    else
                    {
                        this.Invoke(new MethodInvoker(delegate () { listBox1.Items.Add(str); }));


                    }
                    _ocr.Dispose();
                }


                if (pictureBox2.Image == null)
                {

                    pictureBox2.Image = null;
                }

                if (pictureBox3.Image == null)
                {

                    pictureBox3.Image = null;
                }

                if (pictureBox4.Image == null)
                {
                    pictureBox4.Image = null;
                
                }

                if (pictureBox5.Image == null)
                {
                   
                    pictureBox5.Image = null;
                }


                // desposing every element so that for next frame there is no problem 
                newDictionary.Clear();
                Area.Clear();
                bc = null;
                bcb = null;
                noDupes = null;
                SortedList = null;
                semifinalrec = null;
                rec1 = null;
                semifinalrec2 = null;
                redPen.Dispose();

            }
            check++; // frame skipping 
            // showing of frame skipping in textbox 
        
          this.Invoke(new MethodInvoker(delegate () { textBox1.Text = Convert.ToString(check); }));

           




        }


        private void processToolStripMenuItem_Click(object sender, EventArgs e)
        { 
            op.Filter= "Video Files(*.avi;)|*.avi;";
            if (op.ShowDialog() == DialogResult.OK)
            {

          

                FileVideoSource videoSource = new FileVideoSource(op.FileName);
                // set NewFrame event handler
                videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);

                // start the video source
                videoSource.Start();
                // ...

                // signal to stop
                //videoSource.SignalToStop();
            }



         

        }

    }
}
