namespace TheOtherAsync


module Evaluation =
    open Expecto
    open Hopac
    open FSharp.Control.Tasks.V2.ContextInsensitive

    let taskEvaluationTest =
        test "task" {
            let mutable foo = 0
            task {
                foo <- 1000
            } 
            |> ignore
            Expect.equal foo 1000 "Should be 1000"
        }
    let asyncEvaluationTest =
        test "async" {
            let mutable foo = 0
            async {
                foo <- 1000
            } 
            |> ignore
            Expect.equal foo 0 "Should be zero"
        }
    let hopacEvaluationTest =
        test "job" {
            let mutable foo = 0
            job {
                foo <- 1000
            } 
            |> ignore
            Expect.equal foo 0 "Should be zero"
        }
    [<Tests>]
    let tests = testList "Evaluation Tests" [
        taskEvaluationTest
        asyncEvaluationTest
        hopacEvaluationTest
    ]