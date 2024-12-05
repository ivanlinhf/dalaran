namespace Dalaran.Server.Models

open System
open System.Text.Json.Serialization

open Microsoft.SemanticKernel

type ChatMessage =
    { [<JsonPropertyName("id")>]
      Id: string
      [<JsonPropertyName("threadId")>]
      ThreadId: string
      [<JsonPropertyName("content")>]
      Content: ChatMessageContent
      [<JsonPropertyName("timestamp")>]
      Timestamp: DateTime }
