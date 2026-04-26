import type { DetectedObjectDto } from '../types';

class ColorManager {
    private colorMap = new Map<string, string>()
    private usedHues: number[] = []

    getColor(obj: DetectedObjectDto): string{
        const key = this.getStringKey(obj)
        return this.getColorByKey(key)
    }

    getColorByKey(key: string): string{
        if (this.colorMap.has(key)) {
            return this.colorMap.get(key)!
        }

        const hue = this.findDistinctHue()
        const color = `hsl(${hue}, 70%, 50%)`

        this.colorMap.set(key, color)
        this.usedHues.push(hue)

        return color
    }

    getStringKey(obj: DetectedObjectDto) : string{
        return obj.trackId == null ? ""+obj.id : ""+obj.trackId;
    }

    private findDistinctHue(): number {
        if (this.usedHues.length === 0) return 0

        let bestHue = 0
        let maxMinDistance = -1

        for (let h = 0; h < 360; h += 5) {
            const minDistance = Math.min(
                ...this.usedHues.map(u => this.hueDistance(h, u))
            )

            if (minDistance > maxMinDistance) {
                maxMinDistance = minDistance
                bestHue = h
            }
        }

        return bestHue
    }

    private hueDistance(a: number, b: number): number {
        const diff = Math.abs(a - b)
        return Math.min(diff, 360 - diff)
    }

  // optional: reset for tests
    reset() {
        this.colorMap.clear()
        this.usedHues = []
    }
}

export const colorManager = new ColorManager()
