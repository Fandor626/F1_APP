import { setupServer } from 'msw/node'
import { healthHandlers } from '../mocks/handlers/healthHandlers'

export const server = setupServer(...healthHandlers)
