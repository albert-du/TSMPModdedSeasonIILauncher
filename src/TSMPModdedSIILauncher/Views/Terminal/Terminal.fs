namespace TerminalView

// Implementation, see Terminal.fsi

open System
open Avalonia
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Immutable
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types

// Lines are ordered lowest index lowest 
type TerminalSource = 
  { Content: (string * ConsoleColor option) list
    CurrentTyped: string
    CaretPosition: int
    Selected: int option }

module TerminalSource =
    // Create a new Terminal Source with text added
    let private internalWrite (text: string) color newLine (source: TerminalSource) : TerminalSource =
        let newContent =
            // Split up new line chars
            let newLines =
                [ if text.Contains('\n') then yield! List.map (fun j -> j,color) (text.Split('\n')|> List.ofArray) else (text,color) ]
                |> List.rev
            // update first (lowest) line with the last of the new lines
            List.mapi (fun i v -> if i = 0 then (fst(v) + fst(newLines.[^0]), color) else v ) source.Content
            |> List.append newLines.[0 .. ^1] // Append whatever is left
            |> fun i -> if newLine then List.append [("",None)] i else i // Add a new line if required 
        {source with Content = newContent}

    let empty: TerminalSource = {Content = [("",None)]; CurrentTyped = ""; CaretPosition = 0; Selected = None }
    
    let write text source = internalWrite text None false source

    let writeColor text color source = internalWrite text color false source

    let writeLine text source = internalWrite text None true source

    let writeLineColor text color source = internalWrite text color true source
 
    let handleTextInput (args:TextInputEventArgs) (source:TerminalSource) : TerminalSource =
        {source with CurrentTyped = source.CurrentTyped.Insert(source.CaretPosition,args.Text) ; CaretPosition = source.CaretPosition + args.Text.Length }
        
    // pass in event args to use
    let handleKeyInput (args:KeyEventArgs) (source:TerminalSource) : TerminalSource * string option = 
        match args.Key with 
        | Key.Return | Key.Enter -> {source with CurrentTyped = ""; CaretPosition = 0; Selected = None}, Some source.CurrentTyped
        | Key.Back when source.Selected.IsNone -> 
            if source.CurrentTyped <> "" then
                {source with CurrentTyped = source.CurrentTyped.Remove(source.CaretPosition - 1, 1); CaretPosition = source.CaretPosition - 1}, None
            else source, None
        | Key.Back -> {source with CurrentTyped = source.CurrentTyped.Remove(source.CaretPosition - 1, 1); CaretPosition = source.CaretPosition - 1}, None

        | Key.C when args.KeyModifiers.HasFlag(KeyModifiers.Control) -> 
            match source.Selected with
            | None -> source.CurrentTyped
            | Some selected ->
                source.CurrentTyped.[Math.Min(selected, source.CaretPosition) .. Math.Max(selected, source.CaretPosition)]
            |> Application.Current.Clipboard.SetTextAsync
            |> Async.AwaitTask 
            |> Async.Start
            source, None

        | Key.V when args.KeyModifiers.HasFlag(KeyModifiers.Control) ->
            let clipboard = Application.Current.Clipboard.GetTextAsync() |> Async.AwaitTask |> Async.RunSynchronously 
            {source with CurrentTyped = source.CurrentTyped + clipboard ; CaretPosition = source.CaretPosition + clipboard.Length; Selected = None }, None
            
        | Key.Left when source.CaretPosition > 0 && args.KeyModifiers.HasFlag(KeyModifiers.Shift) -> 
            let selected =
                match source.Selected with
                | None -> Some source.CaretPosition
                | Some x -> Some x
            {source with CaretPosition = source.CaretPosition - 1; Selected = selected}, None
        | Key.Right when source.CaretPosition < source.CurrentTyped.Length && args.KeyModifiers.HasFlag(KeyModifiers.Shift) -> 
            let selected =
                match source.Selected with
                | None -> Some source.CaretPosition
                | Some x -> Some x 
            {source with CaretPosition = source.CaretPosition + 1; Selected = selected}, None

        | Key.Left when source.CaretPosition > 0 -> 
            {source with CaretPosition = source.CaretPosition - 1; Selected = None}, None

        | Key.Right when source.CaretPosition < source.CurrentTyped.Length -> 
            {source with CaretPosition = source.CaretPosition + 1; Selected = None}, None

        | x when List.contains x [Key.Left; Key.Right; Key.LeftShift; Key.RightShift; Key.LeftCtrl; Key.RightCtrl] |> not -> {source with Selected = None}, None
        | _ -> source, None

[<AutoOpen>]
module Terminal =
    let defaultForeground = ImmutableSolidColorBrush (Color.Parse("#b8ffe1")) :> IBrush
    let defaultBackground = ImmutableSolidColorBrush (Color.Parse("#1d2342"),0.7) :> IBrush

    let defaultColorConverter = function
        | ConsoleColor.Black ->         "#101116"
        | ConsoleColor.DarkRed ->       "#ff5680"
        | ConsoleColor.DarkGreen ->     "#00ff9c"
        | ConsoleColor.DarkYellow ->    "#fffc58"
        | ConsoleColor.DarkBlue ->      "#00b0ff"
        | ConsoleColor.DarkMagenta ->   "#d57bff"
        | ConsoleColor.DarkCyan ->      "#76c1ff"
        | ConsoleColor.DarkGray ->      "#c7c7c7"
        | ConsoleColor.Gray ->          "#686868"
        | ConsoleColor.Red ->           "#ff6e67"
        | ConsoleColor.Green ->         "#5ffa68"
        | ConsoleColor.Yellow ->        "#fffc67"
        | ConsoleColor.Blue ->          "#6871ff"
        | ConsoleColor.Magenta ->       "#d682ec"
        | ConsoleColor.Cyan ->          "#60fdff"
        | ConsoleColor.White ->         "#ffffff"
        | _ -> failwithf "Could not resolve color" 

    type TerminalAttr =
        | ReadEnabled of bool
        | Source of TerminalSource
        | ColorConverter of (ConsoleColor -> string)
        | Foreground of IBrush
        | Background of IBrush
        | FontSize of double

    type TerminalOptions = {| canRead: bool; source: TerminalSource; colorConverter: (ConsoleColor -> string); foreground: IBrush; background: IBrush; fontSize: double |}

    let create (attributes : TerminalAttr list): IView =
        let attributes = 
            let init: TerminalOptions = {|canRead = false; source = TerminalSource.empty ; colorConverter = defaultColorConverter; foreground = defaultForeground; background = defaultBackground; fontSize = 15.|}
            attributes
            |> List.fold (fun (state: TerminalOptions ) attr -> 
                match attr with 
                | ReadEnabled x ->      {|state with canRead = x|}
                | Source x ->           {|state with source = x|}
                | ColorConverter x ->   {|state with colorConverter = x|}
                | Foreground x ->       {|state with foreground = x|}
                | Background x ->       {|state with background = x|}
                | FontSize x ->         {|state with fontSize = x|} ) init
        ScrollViewer.create[
            ScrollViewer.background attributes.background
            ScrollViewer.allowAutoHide true
            ScrollViewer.verticalScrollBarVisibility ScrollBarVisibility.Auto
            ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Disabled
            ScrollViewer.onScrollChanged(fun e -> let sView = (e.Source :?> ScrollViewer) in if Math.Abs (e.OffsetDelta.Y) <= 30. && sView.Offset.Y > 30. then sView.ScrollToEnd() )
            ScrollViewer.content(
                StackPanel.create[
                    StackPanel.margin 5.
                    StackPanel.children[  
                        let range = if attributes.canRead then (List.rev attributes.source.Content).[0 .. ^1] else List.rev attributes.source.Content
                        for line, color in range do
                            TextBlock.create[
                                TextBlock.text line
                                match color with 
                                | Some x -> TextBlock.foreground (attributes.colorConverter x) 
                                | _ -> TextBlock.foreground attributes.foreground 
                                TextBlock.fontSize attributes.fontSize
                                TextBlock.minHeight attributes.fontSize
                                TextBlock.textWrapping TextWrapping.Wrap
                            ]
                        if attributes.canRead then
                            Grid.create [
                                Grid.rowDefinitions "*"
                                Grid.columnDefinitions "*"
                                Grid.children [
                                    let (text, color) = attributes.source.Content.[0]
                                    let color' =
                                        match color with 
                                        | Some x -> TextBlock.foreground (attributes.colorConverter x) 
                                        | _ -> TextBlock.foreground attributes.foreground 
                                        
                                    WrapPanel.create [
                                        WrapPanel.orientation Orientation.Horizontal
                                        WrapPanel.children [
                                            match attributes.source.Selected with
                                            | None -> 
                                                for character in text + attributes.source.CurrentTyped do
                                                    TextBlock.create [
                                                        TextBlock.text (string character)
                                                        TextBlock.fontSize attributes.fontSize 
                                                        TextBlock.minHeight (attributes.fontSize * 1.3)
                                                        color'
                                                    ]
                                            | Some selected -> 
                                                for character in text + attributes.source.CurrentTyped.[0 .. Math.Min(selected, attributes.source.CaretPosition) - 1] do
                                                    TextBlock.create [
                                                        TextBlock.text (string character)
                                                        TextBlock.fontSize attributes.fontSize 
                                                        TextBlock.minHeight (attributes.fontSize * 1.3)
                                                        color'
                                                    ]
                                                for character in attributes.source.CurrentTyped.[Math.Min(selected, attributes.source.CaretPosition) .. Math.Max(selected, attributes.source.CaretPosition) - 1 ] do
                                                    TextBlock.create [
                                                        TextBlock.background "#ccd4ff"
                                                        TextBlock.text (string character)
                                                        TextBlock.fontSize attributes.fontSize 
                                                        TextBlock.minHeight (attributes.fontSize * 1.3)
                                                        color'
                                                    ]
                                                for character in attributes.source.CurrentTyped.[Math.Max(selected, attributes.source.CaretPosition) .. ] do
                                                    TextBlock.create [
                                                        TextBlock.text (string character)
                                                        TextBlock.fontSize attributes.fontSize 
                                                        TextBlock.minHeight (attributes.fontSize * 1.3)
                                                        color'
                                                    ]
                                        ]
                                    ]
                                    WrapPanel.create [
                                        WrapPanel.orientation Orientation.Horizontal
                                        WrapPanel.children [
                                            for character in text + attributes.source.CurrentTyped.[0 .. attributes.source.CaretPosition - 1] do
                                                TextBlock.create [
                                                    TextBlock.text (string character)
                                                    TextBlock.fontSize attributes.fontSize 
                                                    TextBlock.minHeight (attributes.fontSize * 1.3)
                                                    TextBlock.foreground "Transparent"
                                                ]
                                            TextBlock.create [
                                                TextBlock.text "_"
                                                TextBlock.fontSize attributes.fontSize 
                                                TextBlock.minHeight (attributes.fontSize * 1.3)
                                                TextBlock.foreground attributes.foreground
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                            
                    ]
                ]
            )
            ScrollViewer.verticalOffset Double.PositiveInfinity
        ]
        |> generalize
        
    [<AbstractClass; Sealed>]
    type Terminal private () =
        static member readEnabled (readEnabled: bool) : TerminalAttr = ReadEnabled readEnabled
            
        static member source (terminalSource: TerminalSource) : TerminalAttr = Source terminalSource

        static member colorConverter (func: ConsoleColor -> string) : TerminalAttr = ColorConverter func

        static member foreground (brush: IBrush) : TerminalAttr = Foreground brush
        
        static member foreground (brush: string) : TerminalAttr = brush |> Color.Parse |> ImmutableSolidColorBrush |> Terminal.foreground

        static member background (brush: IBrush) : TerminalAttr = Background brush

        static member background (brush:string) : TerminalAttr = brush |> Color.Parse |> ImmutableSolidColorBrush |> Terminal.background

        static member fontSize (value: double) = FontSize value