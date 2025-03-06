using LLama.Common;
using LLama.Sampling;
using System.Data;




namespace LLama.Rag
{
    public class Rag
    {
        public static async Task Main(string[] args)
        {

            string modelPath = @"C:\Projects\Models\Llama-3.2-1B-Instruct-Q4_0.gguf";
            string startUrl = "https://en.wikipedia.org/wiki/Aluminium_alloy";
            int depth = 0;//Scrape only the webpage provided and no links. 
            int minWordLength = 10; //Specify the minimum number of words in a block that should be scraped.
            bool checkSentences = true;
            bool explodeParagraphs = true;



            //initialiaze web scraper and scrape text elements from webpage
            WebScraper webScraper = await WebScraper.CreateAsync(startUrl, depth);
            List<string> facts = await webScraper.ExtractVisibleTextAsync(minWordLength, checkSentences, explodeParagraphs);


            var modelparams = new ModelParams(modelPath)
            {
                ContextSize = 256, // This can be changed by the user according to memory usage and model capability
                Embeddings = true, // This must be set to true to generate embeddings for vector search
                GpuLayerCount = 0, // Set your number of layers to offload to the GPU here, depending on VRAM available (you can mix CPU with GPU for hybrid inference)
                BatchSize = 128, // This can be changed by the user according to memory usage constraints
                UBatchSize = 128 // This should be changed if the batch size requested by the user is smaller than the default
            };


            var model = await LLamaWeights.LoadFromFileAsync(modelparams);
            var embedder = new LLamaEmbedder(model, modelparams);
            //Console.WriteLine($"\nModel: {fullModelName} from {modelPath} loaded\n");

            
            //Create a data table to store the facts with their embeddings
            DataTable dt = new DataTable();
            dt.Columns.Add("Embedding", typeof(List<float[]>));
            dt.Columns.Add("Original text", typeof(string));
            dt.Columns.Add("Score", typeof(float));

            //create embeddings for each fact in the lsit of facts
            foreach (string fact in facts)
            {

          
                   if (embedder.Context.Tokenize(fact).Count() <= embedder.Context.BatchSize)
                   {
                       var embedding = await embedder.GetEmbeddings(fact);
                       dt.Rows.Add(embedding, fact, 0);
                   }
               

            }

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
                var queryEmbeddings = await embedder.GetEmbeddings(query);



                // Compares embeddings to vector db and ranks by similarity
                foreach (DataRow row in dt.Rows)
                {
                    var factEmbeddings = (List<float[]>)row["Embedding"];
                    row["Score"] = SimilaryMeasures.ComputeSimilarity(queryEmbeddings, factEmbeddings);
                    //scores.Add(new Tuple<double, string>(score, (string)row["OriginalText"]));
                }

                // Get top n matches from vector db
                var n_top_matches = 3;
                var topMatches = dt.AsEnumerable()
                    .OrderByDescending(row => row.Field<Single>("score"))
                    .Take(n_top_matches)
                    .Select(row => row.Field<string>("Original text"))
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
                    MaxTokens = 128,
                    //AntiPrompts = ["DU Llama: Please enter a query:\r\n"],
                    SamplingPipeline = new DefaultSamplingPipeline()
                    {
                        Temperature = 0.25f
                    }

                };

                    // Execute conversation with modified prompt including top n matches
                Console.WriteLine("\nQuerying database and processing with LLM...\n");
                await foreach (var text in  session.ChatAsync(new ChatHistory.Message(AuthorRole.User, prompt),inferenceParams))

                {
                    Console.Write("Response:");
                    Console.Write(text);
                }
                conversation += prompt; // Processing the full conversation is not yet implemented, treats each message as a new conversation at this time
                prompt = "";
                

            }


        }


    }
}
