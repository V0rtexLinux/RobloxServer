using System;
using System.IO;
using System.Net;

namespace RobloxServer.Data
{
    /// <summary>
    /// Sincroniza os arquivos de App_Data (Users.xml, Places.xml, ...) com um blob storage
    /// remoto, para sobreviver ao filesystem efêmero da Vercel (cada instância/cold start tem seu
    /// próprio disco local). Sem efeito nenhum se as variáveis de ambiente não estiverem definidas
    /// -- nesse caso o comportamento continua idêntico ao de antes (só disco local), o que mantém
    /// Windows/IIS e o Raspberry Pi funcionando sem mudança nenhuma.
    ///
    /// Configuração (variáveis de ambiente):
    ///   RS_BLOB_URL   - URL base do storage, ex: "https://blob.vercel-storage.com"
    ///   RS_BLOB_TOKEN - Bearer token com permissão de leitura/escrita
    ///
    /// Protocolo esperado, compatível com a REST API do Vercel Blob e com qualquer storage
    /// simples que aceite:
    ///   GET  {RS_BLOB_URL}/{fileName}   -> 200 com o conteúdo, ou 404 se não existir ainda
    ///   PUT  {RS_BLOB_URL}/{fileName}   -> corpo = bytes do arquivo
    /// </summary>
    public static class RemoteBlobStore
    {
        static readonly string BaseUrl = Environment.GetEnvironmentVariable("RS_BLOB_URL");
        static readonly string Token = Environment.GetEnvironmentVariable("RS_BLOB_TOKEN");

        /// <summary>Falso em ambientes sem as variáveis configuradas (Windows/IIS local, Raspberry Pi).</summary>
        public static bool IsEnabled
        {
            get { return !string.IsNullOrEmpty(BaseUrl) && !string.IsNullOrEmpty(Token); }
        }

        /// <summary>
        /// Baixa o arquivo remoto para o caminho local antes de ler, se ele existir remotamente.
        /// Não lança exceção em caso de falha de rede: nesse caso apenas usa o que já está em disco
        /// local (mais seguro do que travar o site inteiro por um problema momentâneo de rede).
        /// </summary>
        public static void Pull(string fileName, string localPath)
        {
            if (!IsEnabled)
            {
                return;
            }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(CombineUrl(fileName));
                request.Method = "GET";
                request.Headers["Authorization"] = "Bearer " + Token;
                request.Timeout = 5000;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var fileStream = File.Create(localPath))
                {
                    responseStream.CopyTo(fileStream);
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Ainda não existe remotamente (primeira escrita vai criar). Normal.
                    return;
                }
                Logging.Log(LogType.Error, "RemoteBlobStore.Pull falhou para " + fileName + ": " + ex.Message);
            }
            catch (Exception ex)
            {
                Logging.Log(LogType.Error, "RemoteBlobStore.Pull falhou para " + fileName + ": " + ex.Message);
            }
        }

        /// <summary>
        /// Envia o arquivo local para o storage remoto depois de cada Save(). Loga o erro mas não
        /// lança: preferimos manter a escrita local válida a derrubar a requisição do usuário por
        /// uma falha de rede no upload de sincronização.
        /// </summary>
        public static void Push(string fileName, string localPath)
        {
            if (!IsEnabled)
            {
                return;
            }

            try
            {
                byte[] data = File.ReadAllBytes(localPath);
                var request = (HttpWebRequest)WebRequest.Create(CombineUrl(fileName));
                request.Method = "PUT";
                request.Headers["Authorization"] = "Bearer " + Token;
                request.ContentType = "application/xml";
                request.ContentLength = data.Length;
                request.Timeout = 5000;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                using (request.GetResponse())
                {
                }
            }
            catch (Exception ex)
            {
                Logging.Log(LogType.Error, "RemoteBlobStore.Push falhou para " + fileName + ": " + ex.Message);
            }
        }

        static string CombineUrl(string fileName)
        {
            return BaseUrl.TrimEnd('/') + "/" + Uri.EscapeDataString(fileName);
        }
    }
}
