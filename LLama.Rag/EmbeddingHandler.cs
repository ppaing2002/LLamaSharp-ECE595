using Microsoft.EntityFrameworkCore;
using System.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;


namespace LLama.Rag
{
    public class EmbeddingHandler : IEmbeddingHandler
    {

        private LLamaEmbedder Embedder { get; set; }
        
        private readonly AppDbContext _db;
        public EmbeddingHandler(LLamaEmbedder embedder, String storageFolder)
        {
     
            
            Embedder = embedder;
            _db = new AppDbContext(storageFolder);
            bool created = _db.Database.EnsureCreated();
            if (created){  //Ensure DB is created
                Console.WriteLine("Database created successfully!");
            }
            if (!created)
            {
                Console.WriteLine("Database Loaded");
            }
             
        }
        public async Task setEmbeddings(EmbeddingSource source, bool Overwrite)
        {

            // Search for an existing source by SourceType and match either URL if it's a website or title if else.
            EmbeddingSource? existingSource = null;
            if (source.SourceType == "Website")
            {
                existingSource = _db.EmbeddingSources
                    .Include(s => s.Embeddings)
                    .FirstOrDefault(s => s.SourceType == source.SourceType && (s.Url == source.Url));
            }
            else
            {
                existingSource = _db.EmbeddingSources
                    .Include(s => s.Embeddings)
                    .FirstOrDefault(s => s.SourceType == source.SourceType && (s.Title == source.Title));
            }

            if (existingSource != null)
            {
                if (Overwrite)
                {
                    //Get existing embedding records to remove
                    var existingEmbeddings = _db.Embeddings
                    .Where(e => e.SourceId == existingSource.Id)
                    .ToList(); // Fetch as list to modify


                    // Delete the existing source and related embeddings
                    Console.WriteLine($"Updating Embeddings for:({existingSource.SourceType}) - {existingSource.Title}");
                    _db.Embeddings.RemoveRange(existingEmbeddings);
                    _db.EmbeddingSources.Remove(existingSource);
                    _db.SaveChanges();
                    existingSource = null; // Mark as deleted so a new one can be created
                }
                else
                {
                    Console.WriteLine($"Source already exists: ({existingSource.SourceType}) - {existingSource.Title} ");
                }
            }
            //if no existing embeddings arefound or the overwrite condition is set
            if (existingSource == null)
            {
                // Create a new source if not found or if Overwrite was true
                source.DateCreated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                _db.EmbeddingSources.Add(source);
                _db.SaveChanges();
                Console.WriteLine($"New Embedding Source added: {source.Title} ({source.SourceType})");
                existingSource = source; // Now treat this as the found/created source

                //get the embeddings for each text and add to the embeddings table
                foreach (var text in source.TextToEmbed)
                {
                    if (Embedder.Context.BatchSize >= Embedder.Context.Tokenize(text).Length)
                    {
                        
                        Embedding embedding = new Embedding()
                        {
                            SourceId = source.Id,
                            PlainText = text,
                            EmbeddingVector = AveragePoolEmbeddings((List<float[]>)await Embedder.GetEmbeddings(text)) //ConvertEmbeddingsToByteArray()
                        };
                        _db.Embeddings.Add(embedding);
                    }

                }
                _db.SaveChanges();
            }
            

        }
        
        public async Task<DataTable> getAllEmbeddings()
        {
            DataTable dt = new DataTable();

            // Define columns
            dt.Columns.Add("Id", typeof(int)); // Primary Key
            dt.Columns.Add("SourceId", typeof(int)); // Foreign Key
            dt.Columns.Add("PlainText", typeof(string)); // Original sentence
            dt.Columns.Add("EmbeddingVector", typeof(float[])); // Raw byte vector

            // Fetch data from _db asynchronously
            var embeddingsList = await Task.Run(() => _db.Embeddings.ToList());

            // Populate DataTable
            foreach (var embedding in embeddingsList)
            {
                dt.Rows.Add(
                    embedding.Id,
                    embedding.SourceId,
                    embedding.PlainText,
                    embedding.EmbeddingVector
                );
            }

            return dt;
        }
        private static byte[] ConvertEmbeddingsToByteArray(IReadOnlyList<float[]> embeddings)
        {
            // Flatten the list of float arrays and convert each float to a byte array, then concatenate them.
            List<byte> byteList = new List<byte>();

            foreach (var embedding in embeddings)
            {
                foreach (var value in embedding)
                {
                    byte[] byteArray = BitConverter.GetBytes(value);
                    
                    byteList.AddRange(byteArray);
                }
            }
            

            return byteList.ToArray(); // Convert the List<byte> to a byte array.
        }
        private static float[] ConvertByteArrayToEmbeddings(byte[] byteArray)
        {
            return Enumerable.Range(0, byteArray.Length / 4)
                             .Select(i => BitConverter.ToSingle(byteArray, i * 4))
                             .ToArray();
        }


        public Task<DataTable> getEmbeddingsBySourceID(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<DataTable> getAllEmbeddingSources()
        {
            DataTable dt = new DataTable();

            // Define columns
            dt.Columns.Add("Id", typeof(int)); // Primary Key
            dt.Columns.Add("SourceType", typeof(string)); // Source type
            dt.Columns.Add("URL", typeof(string)); // Source URL
            dt.Columns.Add("Title", typeof(string)); // Source Title
            dt.Columns.Add("DateCreated", typeof(string)); // Date of source creation
            dt.Columns.Add("Summary", typeof(string)); // Source Summary
            dt.Columns.Add("SummaryEmbeddings", typeof(string)); // Summary Embeddings
            dt.Columns.Add("KeyWords", typeof(string)); // Applicable Keywords for source
            dt.Columns.Add("KeyWordEmbeddings", typeof(string)); // Keyword Embeddings

            // Fetch data from _db asynchronously
            var sourceList = await Task.Run(() => _db.EmbeddingSources.ToList());

            // Populate DataTable
            foreach (var source in sourceList)
            {
                dt.Rows.Add(
                    source.Id,
                    source.SourceType,
                    source.Url,
                    source.Title,
                    source.Summary,
                    source.SummaryEmbeddings,
                    source.DateCreated,
                    source.KeyWords,
                    source.KeyWordEmbeddings
                );
            }

            return dt;
        }
        private float[] AveragePoolEmbeddings(List<float[]> embeddings)
        {
            if (embeddings == null || embeddings.Count == 0)
                throw new ArgumentException("Embeddings list cannot be null or empty.");

            int vectorSize = embeddings[0].Length;

            // Ensure all embeddings have the same length
            if (embeddings.Any(e => e.Length != vectorSize))
                throw new InvalidOperationException("All embeddings must have the same length.");

            // Compute the average element-wise
            return Enumerable.Range(0, vectorSize)
                             .Select(i => embeddings.Average(e => e[i])) // Average each dimension separately
                             .ToArray();
        }
    } 

    // Embedding Source Data Structure
    public class EmbeddingSource
    {
        public int Id { get; set; } // Primary Key
        public string SourceType { get; set; } = string.Empty; //"Website", Only supports website currently"
        public string Url { get; set; } = string.Empty; // URL if applicable
        public string Title { get; set; } = string.Empty; // Title of the source
        public string Summary { get; set; } = string.Empty; // Title of the source
        public string SummaryEmbeddings { get; set; } = string.Empty; // Title of the source
        internal string DateCreated { get; set; } = string.Empty; // Date of creation
        public string[] KeyWords { get; set; } = Array.Empty<string>(); // Keywords for searching
        public float[] KeyWordEmbeddings { get; set; } = Array.Empty<float>();// Keyword embeddings

        //Navigation property 
        public List<Embedding> Embeddings { get; set; } = new();

        [NotMapped] //properties not added to the database
        public List<string> TextToEmbed { get; set; } = new(); // List of strings to be embedded


    }
    // Embedding Data Structure
    public class Embedding
    {
        public int Id { get; set; } //Primary Key
        internal int SourceId { get; set; } //Foreign Key
        public string PlainText { get; set; } //Plain Text Quote
        public float[] EmbeddingVector { get; set; } //Embedding Vector. Would be good to compress this somehow

        //Navigation property 
        public EmbeddingSource EmbeddingSource { get; set; } = null!;

    }
    // Database Creation and Structuring
    public class AppDbContext : DbContext
    {
        public DbSet<EmbeddingSource> EmbeddingSources { get; set; }
        public DbSet<Embedding> Embeddings { get; set; }

        private readonly string _databasePath;
        // Constructor that accepts a custom path
        public AppDbContext(string databasePath)
        {
            _databasePath = databasePath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={_databasePath}");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmbeddingSource>()
                .HasMany(w => w.Embeddings)
                .WithOne(e => e.EmbeddingSource)
                .HasForeignKey(e => e.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}