using Microsoft.Data.SqlClient;
using System.Globalization;

namespace AdwentureWorks;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            DateTime date = ProcessDate(args);

            double exchangeRate = await GetExchangeRate(date);

            using (SqlConnection connection =
                   new SqlConnection(
                       "Server=stbechyn-sql.database.windows.net;Database=AdventureWorksDW2020;User Id=prvniit;Password=P@ssW0rd!;"))
            {
                connection.Open();

                using (SqlCommand command =
                       new SqlCommand("SELECT EnglishProductName, DealerPrice FROM DimProduct", connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    try
                    {
                        using (StreamWriter writer = new StreamWriter($"{date:yyyyMMdd}-adventureworks.csv"))
                        {
                            writer.WriteLine("Date;EnglishProductName;DealerPriceUSD;DealerPriceCZK");

                            while (reader.Read())
                            {
                                string productName = reader.GetString(0); decimal priceUsd;

                                if (reader.IsDBNull(1))
                                {
                                    Console.WriteLine($"Dealer price not available for product: {productName}. Assigning default value.");
                                    priceUsd = 0; // Replace DEFAULT_VALUE with the default value you want to use
                                }
                                else
                                {
                                    priceUsd = reader.GetDecimal(1);
                                }

                                decimal priceCzk = priceUsd * (decimal)exchangeRate;
                                writer.WriteLine($"{date:dd.MM.yyyy};{productName};{priceUsd};{priceCzk}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"An error occurred while writing to the file: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static DateTime ProcessDate(string[] args)
    {
        DateTime firstDate;
        DateTime nearestFunctioningDate;

        if (args.Length == 0)
        {
            firstDate = DateTime.Now;
        }
        else
        {
            try
            {
                firstDate = DateTime.ParseExact(args[0], "dd.MM.yyyy", CultureInfo.InvariantCulture);
            }
            catch (SystemException)
            {
                return DateTime.Now;
            }

            if (firstDate > DateTime.Now)
            {
                return DateTime.Now;
            }
            if (args.Length == 0)
            {
                firstDate = DateTime.Now;
            }
            else
            {
                try
                {
                    firstDate = DateTime.ParseExact(args[0], "dd.MM.yyyy", CultureInfo.InvariantCulture);
                }
                catch (SystemException)
                {
                    return DateTime.Now;
                }

                if (firstDate > DateTime.Now)
                {
                    return DateTime.Now;
                }
            }
        }

        nearestFunctioningDate = GetNearestFunctioningDate(firstDate);

        return nearestFunctioningDate;
    }

    public static DateTime GetNearestFunctioningDate(DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday)
        {
            return date.AddDays(-1);
        }
        else if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            return date.AddDays(-2);
        }
        else
        {
            return date;
        }
    }

    static async Task<double> GetExchangeRate(DateTime exchangeDate)
    {
        using (HttpClient client = new HttpClient())
        {
            string url =
                $"https://www.cnb.cz/cs/financni-trhy/devizovy-trh/kurzy-devizoveho-trhu/kurzy-devizoveho-trhu/rok.txt?rok={exchangeDate.Year}";
            string response = await client.GetStringAsync(url);

            string[] lines = response.Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith(exchangeDate.ToString("dd.MM.yyyy")))
                {
                    string[] parts = line.Split('|');
                    double exchangeRate = double.Parse(parts[29], CultureInfo.InvariantCulture) / 1000;
                    return exchangeRate;
                }
            }
        }
        throw new Exception("Exchange rate for USD not found.");
    }
}