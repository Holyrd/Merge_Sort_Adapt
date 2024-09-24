using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

class Program
{
	static void Main(string[] args)
	{
		const int SERIES_SIZE_10MB = 1024*1024*7;
		const int SERIES_SIZE_100MB = 75 * 1024 * 1024;
		GenerateTestFile("input.txt", 175000000);//175000000 - 1 Гб
		Stopwatch sw = new Stopwatch();
		string seriesSortedFile = ModifySeriesSort("input.txt", SERIES_SIZE_100MB);
		sw.Start();
		ExternalMergeSort(seriesSortedFile,"output.txt");
		File.Delete(seriesSortedFile);
		//ExternalMergeSort("input.txt", "output.txt");
		sw.Stop();
		Console.WriteLine($"{sw.Elapsed.Minutes} минут, {sw.Elapsed.Seconds} секунд, {sw.Elapsed.Milliseconds} миллисекунд.");
	}

	private static void GenerateTestFile(string fileName, int numElements)//Генератор файлу
	{
		Random rand = new Random();
		using (StreamWriter sw = new StreamWriter(fileName))
		{
			for (int i = 0; i < numElements; i++)
			{
				sw.WriteLine(rand.Next(10000));
			}
		}
	}

	static void ExternalMergeSort(string inputFilePath, string outputFilePath)//Сортування
	{
		string bufferA = "bufferA.txt";
		string bufferB = "bufferB.txt";

		NaturalSplitToFile(inputFilePath, bufferA, bufferB);//Перше розділення на два файла

		while (!IsFileEmpty(bufferA) && !IsFileEmpty(bufferB))//Поки один з файлів не стане порожнім
		{
			string tempOutput = "tempOutput.txt";
			MergeBuffers(bufferA, bufferB, tempOutput);

			NaturalSplitToFile(tempOutput, bufferA, bufferB);//Повторюємо розбиття
		}

		File.Copy(IsFileEmpty(bufferA) ? bufferB : bufferA, outputFilePath, true);//Копіюємо результати з не порожнього буферу

		File.Delete(bufferA);//Знищуємо тимчасові файли
		File.Delete(bufferB);
		File.Delete("tempOutput.txt");
	}

	#region modification

	private static string ModifySeriesSort(string inputFilePath, int bufferSize)//Модифікація щоб мінімальна серія була фіксованого розміру
	{
		string tempFile = "TempInput.txt";
		using (StreamReader reader = new StreamReader(inputFilePath))
		{
			List<int> currentBuffer = new List<int>();//Буфер для серії
			long currentBufferSize = 0;

			string line;
			while ((line = reader.ReadLine()) != null)
			{
				int value = int.Parse(line);
				currentBuffer.Add(value);
				currentBufferSize += sizeof(int);

				if (currentBufferSize >= bufferSize)//Якщо буфер більше фіксованого розміру, він записується в файл
				{
					currentBuffer.Sort();
					WriteBufferToFile(currentBuffer, tempFile);//Запис буферу в тимчасовий файл
					currentBuffer.Clear();
					currentBufferSize = 0;
				}
			}

			if (currentBuffer.Count > 0)//Записуємо залишки в файл як окрему серію
			{
				currentBuffer.Sort();
				WriteBufferToFile(currentBuffer, tempFile);
			}
		}
		return tempFile;
	}

	private static void WriteBufferToFile(List<int> buffer, string filePath)//Запис буферу в файл
	{
		using (StreamWriter writer = new StreamWriter(filePath,true))
		{
			foreach (var number in buffer)
			{
				writer.WriteLine(number);
			}
		}
	}

	#endregion

	static void NaturalSplitToFile(string inputFilePath, string outputFilePath1, string outputFilePath2)//Розділ батьківського файла на буферні
	{
		using (StreamReader reader = new StreamReader(inputFilePath))
		using (StreamWriter writer1 = new StreamWriter(outputFilePath1))
		using (StreamWriter writer2 = new StreamWriter(outputFilePath2))
		{
			string line;
			StreamWriter currentWriter = writer1;
			int? prevValue = null;
			bool isFirstElement = true; 

			while ((line = reader.ReadLine()) != null)
			{
				int value = int.Parse(line);

	
				if (isFirstElement || value >= prevValue)//Перевірка на природність
				{
					currentWriter.WriteLine(value); 
				}
				else//Зміна буферу якщо серія закінчилась
				{
					currentWriter.WriteLine("---");

					currentWriter = currentWriter == writer1 ? writer2 : writer1;

					currentWriter.WriteLine(value);
				}

				prevValue = value;
				isFirstElement = false;
			}

			currentWriter.WriteLine("---");
		}
	}

	static void MergeBuffers(string inputFilePath1, string inputFilePath2, string outputFilePath)//функція зливання двох буферів
	{
		using (StreamReader reader1 = new StreamReader(inputFilePath1))
		using (StreamReader reader2 = new StreamReader(inputFilePath2))
		using (StreamWriter writer = new StreamWriter(outputFilePath))
		{
			string line1 = null;
			string line2 = null;

			bool endOfFile1 = false;
			bool endOfFile2 = false;
			line1 = ReadNextNumberOrSeparator(reader1, ref endOfFile1);//Читаємо перші серії
			line2 = ReadNextNumberOrSeparator(reader2, ref endOfFile2);

			while (!endOfFile1 || !endOfFile2)
			{
				if (line1 == null)//Копіювання залишків якщо одна з серій закінчилась та читаємо наступну пару
				{
					writer.WriteLine(line2);
					WriteRemainingSequence(reader2, writer);
					line1 = ReadNextNumberOrSeparator(reader1, ref endOfFile1);
					line2 = ReadNextNumberOrSeparator(reader2, ref endOfFile2);
				}
				else if (line2 == null)
				{
					writer.WriteLine(line1);
					WriteRemainingSequence(reader1, writer);
					line1 = ReadNextNumberOrSeparator(reader1, ref endOfFile1);
					line2 = ReadNextNumberOrSeparator(reader2, ref endOfFile2); 
				}
				else
				{
					if (int.Parse(line1) <= int.Parse(line2))//Порівнюємо елементи з парних серій
					{
						writer.WriteLine(line1);
						line1 = ReadNextNumberOrSeparator(reader1, ref endOfFile1);
					}
					else
					{
						writer.WriteLine(line2);
						line2 = ReadNextNumberOrSeparator(reader2, ref endOfFile2);
					}
				}
			}

			if (!endOfFile1)//Дописуємо залишки в файлі якщо один закінчився
			{
				WriteRemainingSequence(reader1, writer);
			}

			if (!endOfFile2)
			{
				WriteRemainingSequence(reader2, writer);
			}
		}
	}

	static string ReadNextNumberOrSeparator(StreamReader reader, ref bool endOfFile)//Читання наступного елемента
	{
		string line = reader.ReadLine();

		if (line == null)//Якщо файл закінчився
		{
			endOfFile = true;
			return null;
		}
		else if (line == "---")//Якщо серія закінчилася
		{
			return null;
		}

		return line;
	}

	static void WriteRemainingSequence(StreamReader reader, StreamWriter writer)//Переписування залишків в файлі з методою швидкого запису
	{
		char[] buffer = new char[8192];
		StringBuilder currentBlock = new StringBuilder();  
		string line;

		while ((line = reader.ReadLine()) != null)
		{
			if (line == "---") break; 

			currentBlock.AppendLine(line);

			if (currentBlock.Length >= buffer.Length)
			{
				writer.Write(currentBlock.ToString());
				currentBlock.Clear();
			}
		}

		if (currentBlock.Length > 0)//Запис залишків які не вмістилися в перед останній буфер
		{
			writer.Write(currentBlock.ToString());
		}
	}

	static bool IsFileEmpty(string filePath)//Перевіка файла на порожність
	{
		return new FileInfo(filePath).Length == 0;
	}
}


