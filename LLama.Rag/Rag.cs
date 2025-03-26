using LLama.Common;
using LLama.Sampling;
using System.Data;

namespace LLama.Rag
{
    public class Rag
    {
        public static async Task Main(string[] args)
        {
            //Program Parameters
            string fullModelName = "Llama-3.2-1B-Instruct-Q4_0.gguf";
            string modelPath = @$"C:\Projects\Models\{fullModelName}";
            string embeddingStorage = @$"C:\Projects\Embeddings\Embeddings_{fullModelName}";



            //WebscrapingParameters
            string startUrl = "https://en.wikipedia.org/wiki/Aluminium_alloy"; 
            bool overWrite = false;
            int depth = 0;//Scrape only the webpage provided and links up to depth. 
            int minWordLength = 5; //Specify the minimum number of words in a block that should be scraped.
            bool checkSentences = true;
            bool explodeParagraphs = true;


            var modelparams = new ModelParams(modelPath)
            {
                ContextSize = 1024, // This can be changed by the user according to memory usage and model capability
                GpuLayerCount = 5, // Set your number of layers to offload to the GPU here, depending on VRAM available (you can mix CPU with GPU for hybrid inference)
                BatchSize = 512, // This can be changed by the user according to memory usage constraints
                
            };

            var model = await LLamaWeights.LoadFromFileAsync(modelparams);
            Console.WriteLine($"\nModel: {fullModelName} from {modelPath} loaded\n");

            var embedder = new LLamaEmbedder(model, modelparams);

            //Create instance of embeddingHandler to create and store embeddings
            var embeddingHandler = new EmbeddingHandler(embedder, embeddingStorage);

            //initialiaze web scraper and generate a list of Websites and Scraped Content
            WebScraper ScrapedSites = await WebScraper.CreateAsync(startUrl, depth);


            //use embeddinghandler to store information from sites
            foreach (var Site in ScrapedSites.Websites)
            {
                EmbeddingSource source = new EmbeddingSource()
                {
                    Url = Site.Url,
                    Title = Site.Title,
                    SourceType = "Website",
                    Summary = "",
                    KeyWords =[""],
                    TextToEmbed = await ScrapedSites.ExtractVisibleTextAsync(Site, minWordLength, checkSentences, explodeParagraphs) //Fact Generation
                    
                    
                };
                await embeddingHandler.setEmbeddings(source, overWrite);
             
            }

            //Load the embeddings and embedding sources.
            DataTable embeddings = await embeddingHandler.getAllEmbeddings();


            //Create the context and Interactive Executor needed for chat
            var context = model.CreateContext(modelparams);
            var ex = new InteractiveExecutor(context);
            string prompt = "";
            string conversation = "";
            ChatSession session = new ChatSession(ex);
            Console.WriteLine("\n Please enter a query:\r\n");

            //Chat loop
            while (true)
            {
                string query = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(query)) break; // Easy way to quit out
                var queryEmbeddings = (List<float[]>)await embedder.GetEmbeddings(query);

               
                // Get top n matches from vector db, Comparinges  ranks by similarity
                //var infotable = ComputeSimilarityScores(embeddings, queryEmbeddings);
                var n_top_matches = 5;
                var topMatches = embeddings.AsEnumerable()
                    .OrderByDescending(row => SimilaryMeasures.ComputeSimilarity(queryEmbeddings, row.Field<float[]>("EmbeddingVector")))
                    .Take(n_top_matches)
                    .Select(row => row.Field<string>("PlainText"))
                    .ToList();


                // Prepare prompt with original query and top n facts
                prompt = $"Reply in a conversational manner utilizing the top facts in the prompt to answer only the user's specific question. Be a friendly but concise chatbot (do not offer extra, unrelated info) to help users. Query: {query}\n";
                for (int i = 0; i < topMatches.Count; i++)
                {
                    prompt += $"Fact {i + 1}: {topMatches[i]}\n";
                }
                prompt += "Answer:";

                var inferenceParams = new InferenceParams
                {
                    MaxTokens = 256,
                    AntiPrompts = ["\nDU Llama: Please enter a query:\r\n"],
                    SamplingPipeline = new DefaultSamplingPipeline()
                    {
                        Temperature = 0.25f
                    }

                };

                Console.WriteLine($"The {n_top_matches} top matches to the query are:");
                if (topMatches.Count > 0)
                {
                    for (int i = 0; i < n_top_matches; i++)
                    {
                        Console.WriteLine($"\tMatch {i}:{topMatches[i]}");
                    }
                }
                // Execute conversation with modified prompt including top n matches
                Console.WriteLine("\nQuerying database and processing with LLM...\n");



                await foreach (var text in  session.ChatAsync(new ChatHistory.Message(AuthorRole.User, prompt),inferenceParams))

                {
                    Console.Write(text);
                }
                conversation += prompt; // Processing the full conversation is not yet implemented, treats each message as a new conversation at this time
                prompt = "";
                
            }

            
        }

        private static DataTable ComputeSimilarityScores(DataTable embeddings, List<float[]> queryEmbeddings)
        {
            // Clone the original DataTable structure and add a new "Score" column
            DataTable augmentedTable = embeddings.Clone();
            augmentedTable.Columns.Add("Score", typeof(float));

            // Iterate over each row and compute similarity
            foreach (DataRow row in embeddings.Rows)
            {
                var embeddingVector = row.Field<float[]>("EmbeddingVector");

                // Compute similarity if embedding exists; otherwise, default to -1
                float similarityScore = (float)((embeddingVector != null)
                    ? SimilaryMeasures.ComputeSimilarity(queryEmbeddings, embeddingVector)
                    : -1f); // Assign a default score for missing embeddings

                // Copy the row and set the computed score
                DataRow newRow = augmentedTable.NewRow();
                newRow.ItemArray = row.ItemArray; // Copy all existing columns
                newRow["Score"] = similarityScore; // Add computed similarity score
                augmentedTable.Rows.Add(newRow);
            }

            return augmentedTable;
        }
    }
}
