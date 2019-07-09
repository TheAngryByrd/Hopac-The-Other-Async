namespace TheOtherAsync


module Recursion =
    open Expecto
    open Hopac
    open FSharp.Control.Tasks.V2.ContextInsensitive

    // Tail recursion is not available for Tasks and will cause stackoverflow exceptions
    let rec recursiveTask timesToRun = task {
        if timesToRun = 0 then
            return ()
        else
            return!recursiveTask (timesToRun - 1)
    }


    let taskRecursionTest =
        testTask "task" {
            do! recursiveTask 10000
        }


    // Recursion will need to be rewritten to a while loop 
    let loopTask timesToRun = task {
        let mutable timesLeft = timesToRun
        while timesLeft <> 0 do
            timesLeft <- timesLeft - 1
    }

    let taskLoopTest = 
        testTask "task loop" {
            do! loopTask 10000
        }

    let rec recursiveAsync timesToRun = async {
        if timesToRun = 0 then
            return ()
        else
            return!recursiveAsync (timesToRun - 1)
    }

    let asyncRecursionTest =
        testAsync "async" {
            do! recursiveAsync 10000
        }

    let rec recursiveJob timesToRun = job {
        if timesToRun = 0 then
            return ()
        else
            return!recursiveJob(timesToRun - 1)
    }

    let hopacRecursionTest =
        testJob "job" {
            do! recursiveJob 10000
        }

    [<Tests>]
    let tests = testList "Recursion/Loop Tests" [
        // taskRecursionTest // StackOverflowException.
        taskLoopTest
        asyncRecursionTest
        hopacRecursionTest
    ]