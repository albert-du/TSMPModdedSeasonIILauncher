namespace TSMPModdedSIILauncher.Views

open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Controls.Primitives
open Avalonia.Media

open Elmish
open Avalonia.FuncUI.DSL

open TSMPModdedSIILauncher.Core

module Settings =

    type Msg = 
        | LoadConfig
        | SetMemory of int
        | SetJVMArgs of string
        | SetResolutionWidth of int
        | SetResolutionHeight of int
        | SetUseLiveUpdates of bool
        | SetUseOptifine of bool
        | SetOptifineLink of string
        | SetBranch of string

    type State =
      { jvmArgs: string
        resWidth: int
        resHeight: int
        memory: int
        useLiveUpdates: bool
        useOptifine: bool
        optifineLink: string
        branch: string 
        requiresReinitialization: bool}

    let init () =
      { jvmArgs = ""
        resWidth = 0
        resHeight = 0
        memory = 0
        useLiveUpdates = false
        useOptifine = false
        optifineLink = ""
        branch = ""
        requiresReinitialization = false}, Cmd.ofMsg LoadConfig

    let update (msg: Msg) (state: State) (config:Configuration) : State * Cmd<Msg> =
        let newState =
            match msg with 
            | LoadConfig -> 
                { jvmArgs = config.JVMArgs
                  resWidth = config.ResolutionWidth
                  resHeight = config.ResolutionHeight
                  useLiveUpdates = config.LiveUpdates
                  memory = config.Memory
                  useOptifine = config.Optifine
                  optifineLink = config.OptifineLink
                  branch = config.Branch
                  requiresReinitialization = false}, Cmd.none

            | SetJVMArgs jvmArgs' -> 
                config.JVMArgs <- jvmArgs'
                {state with jvmArgs = jvmArgs'}, Cmd.none
            | SetResolutionWidth width -> 
                config.ResolutionWidth <- width
                {state with resWidth = width}, Cmd.none
            | SetResolutionHeight height ->
                config.ResolutionHeight <- height
                {state with resHeight = height}, Cmd.none
            | SetMemory mb ->
                config.Memory <- mb
                {state with memory = mb}, Cmd.none

            | SetUseLiveUpdates useLiveUpdates' -> 
                config.LiveUpdates <- useLiveUpdates'
                {state with useLiveUpdates = useLiveUpdates'; requiresReinitialization = true}, Cmd.none
            | SetUseOptifine useOptifine' ->
                config.Optifine <- useOptifine'
                {state with useOptifine = useOptifine'; requiresReinitialization = true}, Cmd.none
            | SetOptifineLink optifineLink' ->
                config.OptifineLink <- optifineLink'
                {state with optifineLink = optifineLink'; requiresReinitialization = true}, Cmd.none
            | SetBranch branch' ->
                config.Branch <- branch'
                {state with branch = branch'; requiresReinitialization = true}, Cmd.none
        config.Save()
        newState

    let private textSettingView mainText subtext settingText (onChange: string -> unit) =
        StackPanel.create [
            StackPanel.spacing 5.
            StackPanel.margin 5.
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text mainText
                    TextBlock.fontSize 18.0
                    TextBlock.fontWeight FontWeight.Medium
                ]
                TextBlock.create [
                    TextBlock.text subtext
                    TextBlock.fontSize 15.0
                    TextBlock.fontStyle FontStyle.Oblique
                    TextBlock.fontWeight FontWeight.Normal
                ]
                TextBox.create [
                    TextBox.text (settingText |> string)
                    TextBox.onTextChanged onChange
                ]   
   
            ]
        ]
    let private boolSettingView mainText subtext (settingBool:bool) (onChange: bool -> unit) =
        StackPanel.create [
            StackPanel.spacing 5.
            StackPanel.margin 5.
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text mainText
                    TextBlock.fontSize 18.
                    TextBlock.fontWeight FontWeight.Medium
                ]
                TextBlock.create [
                    TextBlock.text subtext
                    TextBlock.fontSize 15.
                    TextBlock.fontStyle FontStyle.Oblique
                    TextBlock.fontWeight FontWeight.Normal
                ]
                CheckBox.create [
                    CheckBox.isChecked settingBool
                    CheckBox.onChecked (fun _ -> true |> onChange)
                    CheckBox.onUnchecked (fun _ -> false |> onChange)
                ]   
   
            ]
        ]

    let private resolutionView width height dispatch =
        StackPanel.create [
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text "Resolution"
                    TextBlock.fontSize 18.
                    TextBlock.fontWeight FontWeight.Medium
                ]
                TextBlock.create [
                    TextBlock.text "use 0 x 0 for default"
                    TextBlock.fontSize 15.
                    TextBlock.fontStyle FontStyle.Oblique
                    TextBlock.fontWeight FontWeight.Normal
                ]

                StackPanel.create [
                    StackPanel.orientation Orientation.Horizontal
                    StackPanel.children [
                        TextBox.create [
                            TextBox.text (width |> string)
                            TextBox.onTextChanged (fun s -> (try s |> int with | _ -> width) |> SetResolutionWidth |> dispatch )
                        ]

                        TextBlock.create [ TextBlock.text " X "; TextBlock.fontWeight FontWeight.Normal ]

                        TextBox.create [
                            TextBox.text (height |> string)
                            TextBox.onTextChanged (fun s -> (try s |> int with | _ -> height) |> SetResolutionHeight |> dispatch)
                        ]
                    ]
                ]
            ] 
        ]
   
    let view (state: State) dispatch =
        Border.create [
            Border.margin 10.
            Border.borderThickness 2.
            Border.cornerRadius 15.
            Border.horizontalAlignment HorizontalAlignment.Center
            Border.verticalAlignment VerticalAlignment.Center
            Border.background "white"
            Border.child (
                ScrollViewer.create [
                    ScrollViewer.horizontalAlignment HorizontalAlignment.Center
                    ScrollViewer.verticalAlignment VerticalAlignment.Center
                    ScrollViewer.verticalScrollBarVisibility ScrollBarVisibility.Auto
                    ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Disabled
                    ScrollViewer.width 500.
                    ScrollViewer.content (
                        StackPanel.create [
                            StackPanel.children[
                                TextBlock.create [
                                    TextBlock.text "Settings"
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                    TextBlock.foreground "#1d2342"
                                    TextBlock.fontSize 30.
                                    TextBlock.fontWeight FontWeight.DemiBold
                                    TextBlock.margin 10.
                                ]
                                Border.create [
                                    Border.margin 15.
                                    Border.padding 10.
                                    Border.background "lightgray"
                                    Border.cornerRadius 15.
                                    Border.horizontalAlignment HorizontalAlignment.Stretch
                                    Border.child (
                                        StackPanel.create [
                                            StackPanel.children [
                                                TextBlock.create [
                                                    TextBlock.text "Launch"
                                                    TextBlock.foreground "#884dff"
                                                    TextBlock.fontSize 20.
                                                    TextBlock.fontWeight FontWeight.Medium
                                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                ]
                                                textSettingView "Memory (MB)" "Overrided by Xmx/Xms in JVM Args" state.memory (fun s -> (try s |> int with | _ -> state.memory ) |> SetMemory |> dispatch )

                                                textSettingView "JVM Arguments" "Arguments should be separated by spaces." state.jvmArgs (SetJVMArgs >> dispatch)
                                            
                                                resolutionView state.resWidth state.resHeight dispatch
                                            
                                            ]
                                        ]
                                    )
                                ]
                                Border.create [
                                    Border.margin 15.
                                    Border.padding 10.
                                    Border.background "lightgray"
                                    Border.cornerRadius 15.0
                                    Border.horizontalAlignment HorizontalAlignment.Stretch
                                    Border.child (
                                        StackPanel.create [
                                            StackPanel.children [
                                                TextBlock.create [
                                                    TextBlock.text "Installation"
                                                    TextBlock.foreground "#ff3399"
                                                    TextBlock.fontSize 20.
                                                    TextBlock.fontWeight FontWeight.Medium
                                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                ]
                                                boolSettingView "Live Updates" "Use most recent commit, highly recommended" state.useLiveUpdates (SetUseLiveUpdates >> dispatch)

                                                boolSettingView "Use Optifine" "Automatically install optifine" state.useOptifine (SetUseOptifine >> dispatch)

                                                textSettingView "Branch" "The branch of the repository for live updates." state.branch (SetBranch >> dispatch)

                                            ]
                                        ]
                                    )
                                ]
                            ]
                        ]
                    )
                ]
            )
        ]


