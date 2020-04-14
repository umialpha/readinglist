using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ParallelProgramming
{
    class Form1
    {
        // The head of the dataflow network.
        ITargetBlock<string> headBlock = null;
        // Enables the user interface to signal cancellation to the network.
        CancellationTokenSource cancellationTokenSource;
        
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
            });
        }
    }
}
