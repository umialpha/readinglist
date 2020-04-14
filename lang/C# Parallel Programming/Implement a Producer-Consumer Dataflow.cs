class DataflowProducerConsumer
{
	static void produce(ITargetBlock<byte[]> target)
	{
		Random rand = new Random()
		for(int i = 0; i < 100; i++)
		{
			byte[] buffer = new byte[1024];
			rand.NextBytes(buffer);
			target.Post(buffer);
		}
		target.Complete();
	}

	staic async Task<int> ConsumeAsync(IReceivableSouceBlock<byte[]> source)
	{
		int bytesProcessed = 0;
		while(await source.OutputAvailableAsync())
		{
			byte[] data;
			while(source.TryReceive(out data))
			{
				bytesProcessed += data.Length;
			}
		}
		return bytesProcessed;
	}

	static void Main(string[] args)
	{
		var buffer = new BufferBlock<byte[]>();
		var consumer = ConsumeAsync(buffer);
		Task.Run(()=>Produce(buffer));
		consumer.Wait()
		Console.WriteLine("Processed {0} bytes.", consumer.Result)

	}

}
