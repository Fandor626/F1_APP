import { setupServer } from 'msw/node'
import { ergastHandlers } from '../mocks/handlers/ergastHandlers'

export const server = setupServer(...ergastHandlers)
