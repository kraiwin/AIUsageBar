import Darwin

exit(ClaudeBridgeCommand.run(arguments: Array(CommandLine.arguments.dropFirst())) ?? 64)
