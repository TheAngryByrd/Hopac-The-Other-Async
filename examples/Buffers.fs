namespace TheOtherAsync


module Buffers =
    open Expecto
    open Hopac
    open FSharp.Control.Tasks.V2.ContextInsensitive


    let chGiveTest =
        testJob "ch give" {
            let mutable foo = 0
            let myChannel = Ch<_>()
            [1..5]
            |> Seq.map(fun i -> job {
                printfn "giving %d" i 
                do! Ch.give myChannel i
                printfn "gave %d" i 
            }
            )
            |> Job.seqIgnore
            |> start
            
            do!
                [1..5]
                |> Seq.map(fun i -> job {
                    printfn "taking %d" i
                    let! v = Ch.take myChannel
                    printfn "took %d" v
                    foo <- foo + v
                })

                |> Job.seqIgnore
            Expect.equal foo 15 "Should be 15"
        }

    let chSendTest =
        testJob "ch send" {
            let mutable foo = 0
            let myChannel = Ch<_>()
            [1..5]
            |> Seq.map(fun i -> job {
                printfn "sending %d" i 
                do! Ch.send myChannel i
                printfn "sending %d" i 
            }
            )
            |> Job.seqIgnore
            |> start
            
            do!
                [1..5]
                |> Seq.map(fun i -> job {
                    printfn "taking %d" i
                    let! v = Ch.take myChannel
                    printfn "took %d" v
                    foo <- foo + v
                })

                |> Job.seqIgnore
            Expect.equal foo 15 "Should be 15"
        }


    let mailboxTest =
        ftestJob "mailbox send" {
            let mutable foo = 0
            let myBox = Mailbox<_>()
            
            [1..5]
            |> Seq.map(fun i -> job {
                printfn "giving %d" i 
                do! Mailbox.send myBox i
                printfn "gave %d" i 
            }
            )
            |> Job.seqIgnore
            |> start
            
            do!
                [1..5]
                |> Seq.map(fun i -> job {
                    printfn "taking %d" i
                    let! v = Mailbox.take myBox
                    printfn "took %d" v
                    foo <- foo + v
                })

                |> Job.seqIgnore
            Expect.equal foo 15 "Should be 15"
        }

    //Whats the difference between mailbox send and ch send?


    [<Tests>]
    let tests = testList "Buffered Tests" [
        chGiveTest
        chSendTest
        mailboxTest
    ]