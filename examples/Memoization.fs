namespace TheOtherAsync


module Memoization =
    open System
    open System.Threading.Tasks
    open Expecto
    open Hopac
    open FSharp.Control.Tasks.V2.ContextInsensitive

    let getDateTask () = task {
        return DateTimeOffset.UtcNow
    }

    let taskMemoizationTest =
        testTask "task" {
            let getDate = getDateTask ()
            let! d1 = getDate
            do! Task.Delay(1000)
            let! d2 = getDate 
            Expect.equal d1 d2 "Same date"
        }


    let getDateAsync () = async {
        return DateTimeOffset.UtcNow
    }


    let asyncMemoizationTest =
        testAsync "async" {
            let getDate = getDateAsync ()
            let! d1 = getDate
            do! Async.Sleep(1000)
            let! d2 = getDate 
            Expect.notEqual d1 d2 "Different date"
        }


    let getDateJob () = job {
        return DateTimeOffset.UtcNow
    }

    let hopacMemoizationTest =
        testJob "job" {
            let getDate = getDateJob ()
            let! d1 = getDate
            do! timeOutMillis 1000
            let! d2 = getDate 
            Expect.notEqual d1 d2 "Different date"
        }

    let hopacPromiseMemoizationTest =
        testJob "promise" {
            let getDate = getDateJob () |> memo
            let! d1 = getDate
            do! timeOutMillis 1000
            let! d2 = getDate 
            Expect.equal d1 d2 "same date"
        }

    [<Tests>]
    let tests = testList "Memoization Tests" [
        taskMemoizationTest
        asyncMemoizationTest
        hopacMemoizationTest
        hopacPromiseMemoizationTest
    ]