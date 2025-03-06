using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLama;
using LLama.Native;
using System.Text.Json;
using LLama.Common;
using System.Data;

namespace LLama.Rag
{

    class EmbeddingHandler
    {

        public ModelParams ModelParamameters { get; private set; }
        public string FilePath;
        public Dictionary<string, object> Embeddings;
        private bool isLoaded;



        public EmbeddingHandler(string filePath, ModelParams modelParameters)
        {
            ModelParamameters = modelParameters;
            FilePath = filePath;

            LoadEmbeddings();
        }

        public void setEmbeddings(string Source, object Embedder, bool Overwrite)
        {
            if (isLoaded & !Overwrite)
            {
                //check for embedding in loaded file
            }
            else if (isLoaded & Overwrite)
            {
                //if file is loaded and overwrite is enabled, remove old data write new. 
            }
            else
            {
                //write new data to file
            }

        }



        private void LoadEmbeddings()
        {
            if (!File.Exists(FilePath))
            {
                File.Create(FilePath).Close(); // Create an empty file
            }
            else
            {
                string json = File.ReadAllText(FilePath);
            }
        }


    }
}
