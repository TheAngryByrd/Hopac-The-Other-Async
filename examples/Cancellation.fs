namespace TheOtherAsync


module Expect =
    open Expecto
    open Hopac
    open System.Threading
    open System.Threading.Tasks
    open FSharp.Control.Tasks.V2.ContextInsensitive

    let throwsTtask<'texn> (f : unit -> Task<unit>) message = task {
        let! thrown = task {
            try
                do! f ()
                return None
            with e ->
                return Some e
        }
        match thrown with
        | Some e when e.GetType() <> typeof<'texn> ->
            failtestf "%s. Expected f to throw an exn of type %s, but one of type %s was thrown."
                message
                (typeof<'texn>.FullName)
                (e.GetType().FullName)
        | Some _ -> ()
        | _ -> failtestf "%s. Expected f to throw." message
    }


    let throwsTAsync<'texn> (f : Async<unit>) message = async {
        let! thrown = async {
            try
                do! f 
                return None
            with e ->
                return Some e
        }
        match thrown with
        | Some e when e.GetType() <> typeof<'texn> ->
            failtestf "%s. Expected f to throw an exn of type %s, but one of type %s was thrown."
                message
                (typeof<'texn>.FullName)
                (e.GetType().FullName)
        | Some _ -> ()
        | _ -> failtestf "%s. Expected f to throw." message
    }

module Async =
    open System.Threading
    open System.Threading.Tasks

    let withCancellation (ct:CancellationToken) (a:Async<'a>) : Async<'a> = async {
        let! ct2 = Async.CancellationToken
        use cts = CancellationTokenSource.CreateLinkedTokenSource (ct, ct2)
        let tcs = new TaskCompletionSource<'a>()
        use _reg = cts.Token.Register (fun () -> tcs.TrySetCanceled() |> ignore)
        let a = async {
            try
                let! a = a
                tcs.TrySetResult a |> ignore
            with ex ->
                tcs.TrySetException ex |> ignore }
        Async.Start (a, cts.Token)
        return! tcs.Task |> Async.AwaitTask }

module Alt =
    open Hopac
    open Hopac.Infixes
    open System.Threading

    let fromCT (ct : CancellationToken) =
        Alt.withNackJob(fun nack ->
            let cancelled = IVar()
            let sub = ct.Register(fun () ->
                IVar.fill cancelled () |> start)
            nack
            |> Job.map sub.Dispose
            |> Job.start 
            >>-. Alt.tryFinallyFun cancelled sub.Dispose
        )


module Cancellation =
    open System
    open Expecto
    open Hopac
    open System.Threading
    open System.Threading.Tasks
    open FSharp.Control.Tasks.V2.ContextInsensitive


    let taskCancellation (container : ResizeArray<_>) (ct : CancellationToken) = task {
        ct.ThrowIfCancellationRequested()
        container.Add "Doing before task stuff!"
        do! Task.Delay(1000, ct)
        ct.ThrowIfCancellationRequested()
        container.Add "Doing after task stuff!"
    }

    let taskCancellationTest =
        testTask "Task" {
            let container = ResizeArray<_>()
            do! Expect.throwsTtask<TaskCanceledException> (fun () -> task {
                    use cts = new CancellationTokenSource()
                    cts.CancelAfter(100)
                    do! taskCancellation container cts.Token
                }) "Should throw TaskCanceledException"
            Expect.hasLength container 1 "Should only have 1 message"
        }



    let asyncCancellation (container : ResizeArray<_>) = async {
        let! ct = Async.CancellationToken
        container.Add "Doing before async stuff!"
        do! Async.Sleep(1000)
        container.Add "Doing after async stuff!"
    }

    let asyncCancellationTest =
        testAsync "Async"  {
            let container = ResizeArray<_>()

            do! Expect.throwsTAsync<TaskCanceledException> ( async  {
                    use cts = new CancellationTokenSource()
                    cts.CancelAfter(100)
                    do! Async.withCancellation cts.Token (asyncCancellation container)
                }) "Should throw TaskCanceledException"

            Expect.hasLength container 1 "Should only have 1 message"
        }


    let hopacCancellation (container : ResizeArray<_>) = Alt.withNackJob(fun nack -> job {
        container.Add "Doing before hopac stuff!"
        // Since there's no Alt computation expression we have to use the Continuation-passing style
        let actionToPotentiallyCancel = timeOutMillis 1000 |> Alt.afterFun(fun _ -> container.Add "Doing after hopac stuff!")
        return
            Alt.choose [
                actionToPotentiallyCancel
                upcast nack
            ]
        
    })

    let hopacCancellationTest =
        testJob "Hopac"  {
            let container = ResizeArray<_>()
            use cts = new CancellationTokenSource()
            cts.CancelAfter(100)
            do! Alt.choose [
                hopacCancellation container
                Alt.fromCT cts.Token
            ]

            Expect.hasLength container 1 "Should only have 1 message"
        }

    [<Tests>]
    let tests = testList "Cancellation Tests" [
        taskCancellationTest
        asyncCancellationTest
        hopacCancellationTest
    ]