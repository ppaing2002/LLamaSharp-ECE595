using DocumentFormat.OpenXml.Drawing.Charts;
using LLama;
using LLama.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig.Tokenization;
using UglyToad.PdfPig.Tokens;

namespace LLama.Rag.RagTools
{

    public class Chunker
    {
        private LLamaContext Context;
        private int _BatchSize;
        private int _MaxChunkSize;
        private int _ChunkOverlap;
        public Chunker(LLamaContext context, int maxChunkSize = 256, int chunkOverlap = 10)
        {
            Context = context;
            _BatchSize = (int)Context.BatchSize;
            _MaxChunkSize = maxChunkSize;
            _ChunkOverlap = chunkOverlap;
        }

        public List<Chunk> getChunks(SourceFile source)
        {
            if (_MaxChunkSize > _BatchSize)
            {
                _MaxChunkSize = _BatchSize;
            }

         
            var tokens = Context.Tokenize(source.TextContent); 
            var decoder = new StreamingTokenDecoder(Context);

            var chunks = new List<Chunk>();
            for (int i = 0, chunkIndex = 0; i < tokens.Count(); i += (_MaxChunkSize - _ChunkOverlap))
            {
                List<LLamaToken> chunkTokens = tokens.Skip(i).Take(_MaxChunkSize).ToList();
                decoder.AddRange(chunkTokens);
                  

                string chunkText = decoder.Read();

                chunks.Add(new Chunk
                {
                    SourceId = source.Id,
                    SourceURL = source.Url,
                    Text = chunkText,
                    ChunkIndex = chunkIndex++,  
                });

                decoder.Reset();
            }

            return chunks;
        }
        public List<Chunk> getChunks(List<SourceFile> documents)
        {
            var allChunks = new List<Chunk>();

            foreach (var document in documents)
            {
                var chunks = getChunks(document);
                allChunks.AddRange(chunks);
            }

            return allChunks;
        }


    }

}
