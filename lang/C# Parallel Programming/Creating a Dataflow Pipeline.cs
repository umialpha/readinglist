// This document demonstrates a dataflow pipeline that downloads the book The Iliad of Homer from a website and
// searches the text to match individual words with words that reverse the first word's characters. The formation of
// the dataflow pipeline in this document consists of the following steps:
// 1. Create the dataflow blocks that participate in the pipeline.
// 2. Connect each dataflow block to the next block in the pipeline. Each block receives as input the output of the
// previous block in the pipeline.
// 3. For each dataflow block, create a continuation task that sets the next block to the completed state after the
// previous block finishes.
// 4. Post data to the head of the pipeline.
// 5. Mark the head of the pipeline as completed.
// 6. Wait for the pipeline to complete all work.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks.Dataflow;
// Demonstrates how to create a basic dataflow pipeline.
// This program downloads the book "The Iliad of Homer" by Homer from the Web
// and finds all reversed words that appear in that book.
static class Program
{
	static void Main()
	{
		var downloaString = new TransformBlock<string, string>(async uri =>
		{
			Console.WriteLine("Downloading '{0}'...", uri);
			return await new HpptClient().GetStringAsync(uri);
		});
		var createWordList = TransformBlock<string, string[]>(text =>
		{
			Console.WriteLine("Creating word list...");
			char[] tokens = text.Select(c => char.IsLetter(c) ? c : ' ').ToArray();
			text = new string(tokens);
			return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		});
		var filterWordList = new TransformBlock<string[], string[]>(words =>
		{
			Console.WriteLine("Filtering word list...");
			return words.Where(word => word.Length > 3).Distinct().ToArray();
		});
		var findReverseWords = new TransformManyBlock<string[], string>(words => 
		{
			Console.WriteLine("Finding reversed words...");
			var wordsSet = new HashSet<string>(words);
			return from word in words.AsParallel()
				   let reverse = new string(word.Reverse().ToArray())
				   where word != reverse && wordsSet.Contains(reverse)
				   select word;
		});

		var printReverseWords = new ActionBlock<string>(reverseWord =>
		{
			Console.WriteLine("Found reversed words {0}/{1}",
				reversedWord, new string(reversedWord.Reverse().ToArray()));
		});
		// connect the dataflow blocks to form a pipeline
		var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
		downloaString.LinkTo(createWordList, linkOptions);
		createWordList.LinkTo(filterWordList, linkOptions);
		filterWordList.LinkTo(findReversedWords, linkOptions);
		findReversedWords.LinkTo(printReversedWords, linkOptions);
		// Process "The Iliad of Homer" by Homer.
		downloadString.Post("http://www.gutenberg.org/cache/epub/16452/pg16452.txt");
		// Mark the head of the pipeline as complete.
		downloadString.Complete();
		// Wait for the last block in the pipeline to process all messages.
		printReversedWords.Completion.Wait();
	}
}