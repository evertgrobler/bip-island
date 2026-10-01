import Foundation

/// Rules for the children's profiles: up to four on one Mac, each with a name a parent types,
/// an optional age (which sets the starting band) and an animal picture a pre-reader can spot.
public enum ProfileRules {
    public static let maxChildren = 4
    public static let maxNameLength = 20
    public static let ages = 3...10
    /// Animal pictures to choose from, in the order new profiles get them.
    public static let avatars = ["lion", "penguin", "tortoise", "zebra", "giraffe", "elephant", "crab", "rhino"]

    /// A tidy name: trimmed, single spaces, at most `maxNameLength` characters. Nil when empty.
    public static func cleanName(_ raw: String) -> String? {
        let words = raw.split(whereSeparator: \.isWhitespace)
        let joined = words.joined(separator: " ")
        guard !joined.isEmpty else { return nil }
        return String(joined.prefix(maxNameLength)).trimmingCharacters(in: .whitespaces)
    }

    /// The first animal nobody is using yet (or the first one if all are taken).
    public static func freeAvatar(used: [String]) -> String {
        avatars.first { !used.contains($0) } ?? avatars[0]
    }

    public static func canAdd(currentCount: Int) -> Bool {
        currentCount < maxChildren
    }

    /// A known animal, or the first one for anything unexpected in an old save.
    public static func validAvatar(_ id: String?) -> String {
        guard let id, avatars.contains(id) else { return avatars[0] }
        return id
    }
}
