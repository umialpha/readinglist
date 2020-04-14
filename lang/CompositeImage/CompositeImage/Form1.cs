using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;

namespace CompositeImage
{
    public partial class Form1 : Form
    {
        // The head of the dataflow network.
        ITargetBlock<string> headBlock = null;
        // Enables the user interface to signal cancellation to the network.
        CancellationTokenSource cancellationTokenSource;

        public Form1()
        {
            InitializeComponent();
        }

        // Creates the image preocessing dataflow network and returns the
        // head node of the network
        ITargetBlock<string> CreateImageProcessingNetwork()
        {
            //
            // Create the dataflow blocks that form the network
            //

            // Create a dataflow block that take a folder path as input
            // and returns a collection of Bitmap objects
            var loadBitmaps = new TransformBlock<string, IEnumerable<Bitmap>>(path =>
            {
                try
                {
                    return LoadBitmaps(path);
                }
                catch (OperationCanceledException)
                {
                    return Enumerable.Empty<Bitmap>();
                }
            });

            // Create a dataflow block that takes a collection of Bitmap objects
            // and returns a single composite bitmap
            var createCompositeBitmap = new TransformBlock<IEnumerable<Bitmap>, Bitmap>(bitmaps =>
            {
                try
                {
                    return CreateCompositeBitmap(bitmaps);
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation by passing null to the next stage
                    // of the network.
                    return null;
                }

            });

            // Create a dataflow block that displays the provided bitmap on the form.
            var displayCompositeBitmap = new ActionBlock<Bitmap>(bitmap =>
            {
                // Display the bitmap.
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox1.Image = bitmap;
                // Enable the user to select another folder.
                toolStripButton1.Enabled = true;
                toolStripButton2.Enabled = false;
                Cursor = DefaultCursor;
            },
            // Specify a task scheduler from the current synchronization context
            // so that the action runs on the UI thread.
            new ExecutionDataflowBlockOptions
            {
                TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext()
            });
            // Create a dataflow block that responds to a cancellation request by
            // displaying an image to indicate that the operation is cancelled and
            // enables the user to select another folder.
            var operationCancelled = new ActionBlock<object>(delegate
            {
                // Display the error image to indicate that the operation
                // was cancelled.
                pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
                pictureBox1.Image = pictureBox1.ErrorImage;
                // Enable the user to select another folder.
                toolStripButton1.Enabled = true;
                toolStripButton2.Enabled = false;
                Cursor = DefaultCursor;
            },
            // Specify a task scheduler from the current synchronization context
            // so that the action runs on the UI thread.
            new ExecutionDataflowBlockOptions
            {
                TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext()
            });

            //
            // Connect the network.
            //

            loadBitmaps.LinkTo(createCompositeBitmap, bitmaps => bitmaps.Count() > 0);
            loadBitmaps.LinkTo(operationCancelled);
            createCompositeBitmap.LinkTo(displayCompositeBitmap, bitmap => bitmap != null);
            createCompositeBitmap.LinkTo(operationCancelled);
            return loadBitmaps;
        }


        IEnumerable<Bitmap> LoadBitmaps(string path)
        {
            List<Bitmap> bitmaps = new List<Bitmap>();
            // Load a variety of image types.
            foreach (string bitmapType in
            new string[] { "*.bmp", "*.gif", "*.jpg", "*.png", "*.tif" })
            {
                // Load each bitmap for the current extension.
                foreach (string fileName in Directory.GetFiles(path, bitmapType))
                {
                    // Throw OperationCanceledException if cancellation is requested.
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    try
                    {
                        // Add the Bitmap object to the collection.
                        bitmaps.Add(new Bitmap(fileName));
                    }
                    catch (Exception)
                    {
                        // TODO: A complete application might handle the error.
                    }
                }
            }
            return bitmaps;
        }
        Bitmap CreateCompositeBitmap(IEnumerable<Bitmap> bitmaps)
        {
            Bitmap[] bitmapArray = bitmaps.ToArray();
            // Compute the maximum width and height components of all
            // bitmaps in the collection.
            Rectangle largest = new Rectangle();
            foreach (var bitmap in bitmapArray)
            {
                if (bitmap.Width > largest.Width)
                    largest.Width = bitmap.Width;
                if (bitmap.Height > largest.Height)
                    largest.Height = bitmap.Height;
            }
            var result = new Bitmap(largest.Width, largest.Height, PixelFormat.Format32bppArgb);

            // Lock the result Bitmap.
            var resultBitmapData = result.LockBits(
                new Rectangle(new Point(), result.Size), ImageLockMode.WriteOnly,
                result.PixelFormat);
            // Lock each source bitmap to create a parallel list of BitmapData objects.
            var bitmapDataList = (from b in bitmapArray
                                  select b.LockBits(
                                  new Rectangle(new Point(), b.Size),
                                  ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            .ToList();

            // Compute each column in parallel.
            Parallel.For(0, largest.Width, new ParallelOptions
            {
                CancellationToken = cancellationTokenSource.Token
            },
            i =>
            {
                for (int j = 0; j < largest.Height; j++)
                {
                    int count = 0;
                    int a = 0, r = 0, g = 0, b = 0;
                    foreach(var bitmapData in bitmapDataList)
                    {
                        if (bitmapData.Width > i && bitmapData.Height > j)
                        {
                            unsafe
                            {
                                byte* row = (byte*)(bitmapData.Scan0 + (j * bitmapData.Stride));
                                byte* pix = (byte*)(row + (4 * i));
                                a += *pix; pix++;
                                r += *pix; pix++;
                                g += *pix; pix++;
                                b += *pix;
                            }
                            count++;
                        }
                    }
                    //prevent divide by zero in bottom right pixelless corner
                    if (count == 0)
                        break;
                    unsafe
                    {
                        // Compute the average of each color component.
                        a /= count;
                        r /= count;
                        g /= count;
                        b /= count;
                        byte* row = (byte*)(resultBitmapData.Scan0 + (j * resultBitmapData.Stride));
                        byte* pix = (byte*)(row + (4 * i));
                        *pix = (byte)a; pix++;
                        *pix = (byte)r; pix++;
                        *pix = (byte)g; pix++;
                        *pix = (byte)b;
                    }


                }
                
            }
            );
            // Unlock the source bitmaps.
            for (int i = 0; i < bitmapArray.Length; i++)
            {
                bitmapArray[i].UnlockBits(bitmapDataList[i]);
            }
            // Unlock the result bitmap.
            result.UnlockBits(resultBitmapData);
            // Return the result.
            return result;
        }

    }
        
}
