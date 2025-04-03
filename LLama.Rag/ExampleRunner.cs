
using Spectre.Console;
using System.Collections.Generic;
using LLama.Rag.Examples;


namespace LLama.Rag { 
    class ExampleRunner
    {
        static async Task Main()
        {

            //dictionary for programs selection
            Dictionary<String, Func<Task>> Programs = new()
            {
                { "Local Document RAG",LocalDocumentRag.Run},
                { "Web Search RAG",WebSearchRag.Run},
                
                { "Exit", () => { Environment.Exit(0); return Task.CompletedTask; } }

            };

            AnsiConsole.Write(new Rule("Programs"));

            while (true)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Please choose[green] an example[/] to run: ")
                        .AddChoices(Programs.Keys));

                if (Programs.TryGetValue(choice, out var program))
                {
                    AnsiConsole.Write(new Rule(choice));
                    await program();
                }

                AnsiConsole.Reset();
                AnsiConsole.MarkupLine("Press ENTER to go to the main menu...");
                Console.ReadLine();

                AnsiConsole.Clear();
            }
        }
    }
}