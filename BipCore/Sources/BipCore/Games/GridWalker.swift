/// Runs a Bip's Path program on its grid: one step at a time, exactly as the scene plays it.
/// Absolute blocks (up, down, left, right) move regardless of facing; forward steps the way Bip
/// faces; turnLeft and turnRight only turn him. Walking into a rock or off the grid fails, and
/// Bip must finish on the battery: passing over it doesn't count.
public enum GridWalker {
    public enum Facing: String {
        case up, right, down, left

        func turned(left: Bool) -> Facing {
            let order: [Facing] = [.up, .right, .down, .left]
            let at = order.firstIndex(of: self)!
            return order[(at + (left ? 3 : 1)) % 4]
        }

        var step: (rows: Int, cols: Int) {
            switch self {
            case .up: return (-1, 0)
            case .down: return (1, 0)
            case .left: return (0, -1)
            case .right: return (0, 1)
            }
        }

        init(_ text: String) {
            switch text {
            case "right": self = .right
            case "down": self = .down
            case "left": self = .left
            default: self = .up
            }
        }
    }

    /// Which way Bip faces before the first block and after each block that ran, matching
    /// `path`'s positions one for one (so the scene can turn him as he walks).
    public static func facings(program: [String], on level: GridLevel) -> [Facing] {
        let walked = path(program: program, on: level).positions.count
        var facing = Facing(level.startFacing)
        var out = [facing]
        for block in program where out.count < walked {
            if block == "turnLeft" { facing = facing.turned(left: true) }
            if block == "turnRight" { facing = facing.turned(left: false) }
            out.append(facing)
        }
        return out
    }

    /// Whether the program lands Bip on the goal without crashing.
    public static func reachesGoal(program: [String], on level: GridLevel) -> Bool {
        let result = path(program: program, on: level)
        return !result.crashed && result.positions.last == level.goal
    }

    /// Every square Bip stands on, starting square first. `crashed` is true when a block is
    /// unknown or Bip walks into a rock or off the grid; the positions stop where he crashed.
    /// The scene steps through these one at a time with a highlight on the current block.
    public static func path(program: [String], on level: GridLevel) -> (positions: [GridPosition], crashed: Bool) {
        var at = level.start
        var facing = Facing(level.startFacing)
        var positions = [at]
        for block in program {
            switch block {
            case "up": at = GridPosition(row: at.row - 1, column: at.column)
            case "down": at = GridPosition(row: at.row + 1, column: at.column)
            case "left": at = GridPosition(row: at.row, column: at.column - 1)
            case "right": at = GridPosition(row: at.row, column: at.column + 1)
            case "forward":
                let step = facing.step
                at = GridPosition(row: at.row + step.rows, column: at.column + step.cols)
            case "turnLeft": facing = facing.turned(left: true)
            case "turnRight": facing = facing.turned(left: false)
            default: return (positions, true)
            }
            if block == "turnLeft" || block == "turnRight" {
                positions.append(at)
                continue
            }
            if at.row < 0 || at.column < 0 || at.row >= level.grid.rows || at.column >= level.grid.cols {
                return (positions, true)
            }
            if level.rocks.contains(at) { return (positions, true) }
            positions.append(at)
        }
        return (positions, false)
    }
}
