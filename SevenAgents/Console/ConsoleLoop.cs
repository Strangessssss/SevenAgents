using SevenAgents.Agents;

namespace SevenAgents.Console;

public static class ConsoleLoop
{
    public static async Task RunAsync(Agent agent)
    {
        System.Console.WriteLine("Type a message. 'exit' to quit.\n");

        while (true)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.Write("You > ");
            System.Console.ResetColor();

            var input = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            try
            {
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.Write("AI  > ");
                System.Console.ResetColor();

                var firstToken = true;

                await agent.SayAsync(input, onToken: token =>
                {
                    if (firstToken)
                        firstToken = false;

                    System.Console.Write(token);
                    return Task.CompletedTask;
                });

                System.Console.WriteLine("\n");
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"\nError: {ex.Message}\n");
                System.Console.ResetColor();
            }
        }

        System.Console.WriteLine("Goodbye!");
    }
}