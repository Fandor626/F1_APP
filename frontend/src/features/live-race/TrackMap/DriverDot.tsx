const SECTOR_COLOURS: Record<string, string> = {
  purple: '#bf00ff',
  green: '#00d2be',
  yellow: '#ffd700',
  white: '#e0e0e0',
}

interface DriverDotProps {
  driverCode: string
  teamColour: string
  svgX: number
  svgY: number
  miniSectorStatus: 'purple' | 'green' | 'yellow' | 'white' | null
}

export function DriverDot({ driverCode, teamColour, svgX, svgY, miniSectorStatus }: DriverDotProps) {
  const strokeColour = miniSectorStatus ? (SECTOR_COLOURS[miniSectorStatus] ?? '#0c0e11') : '#0c0e11'
  const strokeWidth = miniSectorStatus ? 3 : 2

  return (
    <g transform={`translate(${svgX},${svgY})`} data-testid={`driver-dot-${driverCode}`}>
      <circle r={9} fill={`#${teamColour}`} stroke={strokeColour} strokeWidth={strokeWidth} />
      <text
        textAnchor="middle"
        dominantBaseline="central"
        fontSize={7}
        fontWeight={700}
        fill="#0c0e11"
      >
        {driverCode}
      </text>
    </g>
  )
}
