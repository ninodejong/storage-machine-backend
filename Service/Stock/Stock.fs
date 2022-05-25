/// This module exposes use-cases of the Stock component as an HTTP Web service using Giraffe.
module StorageMachine.Stock.Stock

open FSharp.Control.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open Thoth.Json.Net
open Thoth.Json.Giraffe
open Stock
open StorageMachine.Stock.Serialization

/// An overview of all bins currently stored in the Storage Machine.
let binOverview (next: HttpFunc) (ctx: HttpContext) =
    task {
        let dataAccess = ctx.GetService<IStockDataAccess> ()
        let bins = Stock.binOverview dataAccess
        return! ThothSerializer.RespondJsonSeq bins Serialization.encoderBin next ctx 
    }

/// An overview of actual stock currently stored in the Storage Machine. Actual stock is defined as all non-empty bins.
let stockOverview (next: HttpFunc) (ctx: HttpContext) =
    task {
        let dataAccess = ctx.GetService<IStockDataAccess> ()
        let bins = Stock.stockOverview dataAccess
        return! ThothSerializer.RespondJsonSeq bins Serialization.encoderBin next ctx 
    }

/// An overview of all products stored in the Storage Machine, regardless what bins contain them.
let productsInStock (next: HttpFunc) (ctx: HttpContext) =
    task {
//        let productsOverview = Stock.productsInStock (failwith "Exercise 0: fill this in to complete this HTTP handler.")
        let dataAccess = ctx.GetService<IStockDataAccess> ()
        let productsOverview = Stock.productsInStock dataAccess
        return! ThothSerializer.RespondJson productsOverview Serialization.encoderProductsOverview next ctx 
    }
    
let addBin (next: HttpFunc) (ctx: HttpContext) =
    task {
        let dataAccess = ctx.GetService<IStockDataAccess> ()
        
        let! bin = ThothSerializer.ReadBody ctx decoderBin
        match bin with
        | Error error ->
            return! RequestErrors.badRequest (text error) earlyReturn ctx
        | Ok bin ->
            match Stock.storeBin bin dataAccess with
            | Ok(_) ->
                return! ThothSerializer.RespondJson bin Serialization.encoderBin next ctx
            | Error error ->
                return! RequestErrors.badRequest (text error) earlyReturn ctx
          
    }

/// Defines URLs for functionality of the Stock component and dispatches HTTP requests to those URLs.
let handlers : HttpHandler =
    choose [
        GET >=> route "/bins" >=> binOverview
        GET >=> route "/stock" >=> stockOverview
        GET >=> route "/stock/products" >=> productsInStock
        POST >=> route "/stock" >=> addBin
    ]