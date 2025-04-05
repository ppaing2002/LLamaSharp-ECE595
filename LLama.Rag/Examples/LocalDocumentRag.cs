using LLama.Common;
using LLama.Sampling;
using System.Data;
using LLama.Rag.RagTools;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Diagnostics;


namespace LLama.Rag.Examples
{
    public class LocalDocumentRag
    {
        public static async Task Run()
        {
            //Program Parameters
            string fullModelName = "Llama-3.2-1B-Instruct-Q4_0.gguf";
            string modelPath = @$"C:\Projects\Models\{fullModelName}";


            //Local document file location and embedding storage location
            string StorageFolder = @$"C:\Projects\SourceData\LocalDocRAG_{fullModelName}"; //folder to store the embeddings
            string fileFolder = "\\\\global.scd.scania.com\\home\\Se\\016\\SSHZW5\\Documents\\Schoolwork\\4 - Spring 2025\\Research Papers"; //your file folder here
            bool overWrite = false; //Always overwrite embeddings. 
       

            //Define model parameters and intitialize model
            var modelparams = new ModelParams(modelPath)
            {
                ContextSize = 1024, // This can be changed by the user according to memory usage and model capability
                GpuLayerCount = 5, // Set your number of layers to offload to the GPU here, depending on VRAM available (you can mix CPU with GPU for hybrid inference)
                BatchSize = 512, // This can be changed by the user according to memory usage constraints

            };

            //Initialize model and context for use in chunker
            var model = await LLamaWeights.LoadFromFileAsync(modelparams);
            var context = model.CreateContext(modelparams);

            //Create Instance of chunker
            Chunker chunker = new Chunker(context, 256, 10); //maxChunkSize = 256 tokens , chunk overlap = 10

            //Create instance of embedder if chunks need to be embedded
            LLamaEmbedder embedder = new LLamaEmbedder(model, modelparams);

            //Instantiate storage handler using folder, optional: chunker,  optional: embedder, embedder is required here for current matching method
            StorageHandler storageHandler = new StorageHandler(StorageFolder,chunker, embedder);

            //Parse files from file folder using DocumentParser
            List<SourceFile> documents = DocumentParser.ParseFolder(fileFolder);


            //The documents can be stored with the using the StorageHandler
            foreach (var source in documents)
            {
                /* Summary & keyword embeddings for sources
                 * var summary = LLM_Agent.Summarize(source.TextContent)
                 * source.Summary = summary
                 * source.SummaryEmbeddings = embedder.GetEmbeddings(summary)
                 * 
                 * var keywords = LLM_Agent.GetKeywords
                 * source.Keywords = keywords
                 * source.KeywordEmbeddings = embedder.GetEmbeddings(Keywords)
                */

                await storageHandler.SaveSource(source, overWrite);
            }


            Console.WriteLine($"\nModel: {fullModelName} from {modelPath} loaded\n");


            DataTable allSources = await storageHandler.GetAllSources();
            DataTable allChunks = await storageHandler.GetAllChunks();

            //Create the Interactive Executor needed for chat

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
                var queryEmbeddings = (List<float[]>) await embedder.GetEmbeddings(query);

               
                // Get top n matches from vector db, Comparinges  ranks by similarity
                //var infotable = ComputeSimilarityScores(embeddings, queryEmbeddings);
                var n_top_matches = 5;
                var topMatches = allChunks.AsEnumerable()
                    .OrderByDescending(row => SimilaryMeasures.ComputeSimilarity(queryEmbeddings, row.Field<float[]>("Embedding")))
                    .Take(n_top_matches)
                    .Select(row => row.Field<string>("Text"))
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
    }
}
