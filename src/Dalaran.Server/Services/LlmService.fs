namespace Dalaran.Server.Services

type ILlmService =
    abstract Chat: unit -> unit

type LlmService () =
    interface ILlmService with
        member this.Chat () = raise (System.NotImplementedException ())
