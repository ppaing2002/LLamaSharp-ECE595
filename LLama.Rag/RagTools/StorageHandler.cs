using Microsoft.EntityFrameworkCore;
using System.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using Microsoft.Extensions.AI;



namespace LLama.Rag.RagTools
{
    public class StorageHandler : IStorageHandler
    {
        private readonly Chunker? _chunker;
        private readonly LLamaEmbedder? _embedder;
        private readonly AppDbContext _db;



        // Basic constructor (source only)
        public StorageHandler(string storageFolder)
        {
            _db = new AppDbContext(storageFolder);
            var created = _db.Database.EnsureCreated();
            if (created) {  //Ensure DB is created
                Console.WriteLine("Database created successfully!");
            }
            if (!created)
            {
                Console.WriteLine("Database Loaded");
            }
        }
        // Constructor with chunking support only
        public StorageHandler(string storageFolder, Chunker chunker)
            : this(storageFolder)
        {
            _chunker = chunker;
        }
        //Constructor with embedding support only
        public StorageHandler(string storageFolder, LLamaEmbedder embedder)
            : this(storageFolder)
        {
            _embedder = embedder;
        }

        // Constructor with full chunking + embedding support
        public StorageHandler(string storageFolder, Chunker chunker, LLamaEmbedder embedder)
            : this(storageFolder, chunker)
        {
            _embedder = embedder;
        }


        public async Task SaveSource(SourceFile source, bool overwrite)
        {
            var existingSource = _db.Sources.Include(s => s.Chunks)
                .FirstOrDefault(s => s.SourceType == source.SourceType && s.Url == source.Url);

            if (existingSource != null && overwrite)
            {
                Console.WriteLine($"Updating Embeddings for: {existingSource.FileName}");
                _db.Chunks.RemoveRange(existingSource.Chunks);
                _db.Sources.Remove(existingSource);
                _db.SaveChanges();
                existingSource = null;
            }

            if (existingSource == null)
            {
                source.DateLastSaved = DateTime.UtcNow;
                _db.Sources.Add(source);
                _db.SaveChanges();
                Console.WriteLine($"New Embedding Source added: {source.FileName}");
                existingSource = source;

                //if no chunker or embedder, save only the source. Done above

                //if no embedder is provided save the source and it's chunks
                if (_chunker != null && _embedder == null)
                {
                    var chunks = _chunker.getChunks(source);
                    _db.Chunks.AddRange(chunks);
                    _db.SaveChanges();
                    Console.WriteLine($"\tSaved {chunks.Count} chunks for: {source.FileName}");
                }
                //if embedder is provided without a chunker
                else if (_chunker == null && _embedder != null)
                {
                    if (source.TextContentList == null)
                    {
                        throw new Exception("If not using chunker to create chunks, SourceFile must have a non-null 'TextContentList' Parameter");
                    }
                    int index = 0;
                    foreach (string text in source.TextContentList)
                    {
                        var embedding = (List<float[]>)await _embedder.GetEmbeddings(text);
                        Chunk chunk = new Chunk()
                        {
                            SourceId = source.Id,
                            SourceURL = source.Url,
                            ChunkIndex = index,
                            Text = text,
                            Embedding = AveragePoolEmbeddings(embedding)
                        };
                        _db.Chunks.Add(chunk);
                        index++;
                    }
                    
                    _db.SaveChanges();
                    Console.WriteLine($"\tSaved {index} chunks for: {source.FileName}");
                }
                //If both a chunker and embedder are provided, save the source and the embedded chunks
                else if (_chunker != null && _embedder != null)
                {
                    var chunks = _chunker.getChunks(source);
                    foreach (var chunk in chunks)
                    {
                        var embeddings = (List<float[]>)await _embedder.GetEmbeddings(chunk.Text);
                        chunk.Embedding = AveragePoolEmbeddings(embeddings);
                        _db.Chunks.Add(chunk);
                    }
                    _db.SaveChanges();
                    Console.WriteLine($"\tSaved {chunks.Count} chunks for: {source.FileName}");
                }
                else
                {

                }
            }
            else
            {
                Console.WriteLine($"Source already exists: ({existingSource.SourceType}) - {existingSource.FileName} ");
            }
        }

        public async Task<DataTable> GetAllSources()
        {
            var dt = new DataTable();

            // Define columns
            dt.Columns.Add("Id", typeof(int)); // Primary Key
            dt.Columns.Add("SourceType", typeof(string)); // Source type
            dt.Columns.Add("URL", typeof(string)); // Source URL
            dt.Columns.Add("FileName", typeof(string)); // Source Title
            dt.Columns.Add("FileType", typeof(string)); // Source URL
            dt.Columns.Add("DateLastSaved", typeof(DateTime)); // Date of source last edit
            dt.Columns.Add("Summary", typeof(string)); // Source Summary
            dt.Columns.Add("SummaryEmbeddings", typeof(string)); // Summary Embeddings
            dt.Columns.Add("KeyWords", typeof(string)); // Applicable Keywords for source
            dt.Columns.Add("KeyWordEmbeddings", typeof(string)); // Keyword Embeddings

            // Fetch data from _db asynchronously
            var sourceList = await Task.Run(() => _db.Sources.ToList());

            // Populate DataTable
            foreach (var source in sourceList)
            {
                //This format needs to match the format outlined in the SourceFile Class
                dt.Rows.Add(
                    source.Id,
                    source.SourceType,
                    source.Url,
                    source.FileName,
                    source.FileType,
                    source.DateLastSaved,
                    source.Summary,
                    source.SummaryEmbeddings,
                    source.KeyWords,
                    source.KeyWordEmbeddings
                );
            }

            return dt;
        }
        public async Task<DataTable> GetAllChunks()
        {
            var dt = new DataTable();

            // Define columns
            dt.Columns.Add("Id", typeof(int)); // Primary Key
            dt.Columns.Add("SourceId", typeof(int)); // Foreign Key
            dt.Columns.Add("SourceURL", typeof(string)); // Original sentence
            dt.Columns.Add("ChunkIndex", typeof(int)); // Foreign Key
            dt.Columns.Add("Text", typeof(string)); // Original sentence
            dt.Columns.Add("Embedding", typeof(float[])); // Raw byte vector

            // Fetch data from _db asynchronously
            var chunkList = await Task.Run(() => _db.Chunks.ToList());

            // Populate DataTable
            foreach (var chunk in chunkList)
            {
                dt.Rows.Add(
                    chunk.Id,
                    chunk.SourceId,
                    chunk.SourceURL,
                    chunk.ChunkIndex,
                    chunk.Text,
                    chunk.Embedding
                );
            }

            return dt;
        }
        public Task<DataTable> GetChunksBySourceID(int id)
        {
            throw new NotImplementedException();
        }
       
        private float[] AveragePoolEmbeddings(List<float[]> embeddings)
        {
            if (embeddings == null || embeddings.Count == 0)
                throw new ArgumentException("Embeddings list cannot be null or empty.");

            var vectorSize = embeddings[0].Length;

            // Ensure all embeddings have the same length
            if (embeddings.Any(e => e.Length != vectorSize))
                throw new InvalidOperationException("All embeddings must have the same length.");

            // Compute the average element-wise
            return Enumerable.Range(0, vectorSize)
                             .Select(i => embeddings.Average(e => e[i])) // Average each dimension separately
                             .ToArray();
        }
    }


    // Database Creation and Structuring
    public class AppDbContext : DbContext
    {
        public DbSet<SourceFile> Sources { get; set; }
        public DbSet<Chunk> Chunks { get; set; }

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
            modelBuilder.Entity<SourceFile>()
                .HasMany(w => w.Chunks)
                .WithOne(e => e.Source)
                .HasForeignKey(e => e.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}