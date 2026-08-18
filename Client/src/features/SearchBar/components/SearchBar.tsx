// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import Autocomplete, { createFilterOptions } from '@mui/material/Autocomplete'
import Box from '@mui/material/Box'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Tooltip from '@mui/material/Tooltip'
import {
	Children,
	HTMLAttributes,
	ReactElement,
	cloneElement,
	createContext,
	forwardRef,
	memo,
	useContext,
} from 'react'
import { FixedSizeList, ListChildComponentProps } from 'react-window'

/**
 * Props for the SearchBar component.
 */
type SearchBarProps = {
	projectNames: string[]
	setSelectedProject: (projectName: string) => void
}

const MAX_VISIBLE_OPTIONS = 8
const OPTION_HEIGHT = 44
const LISTBOX_PADDING = 8
const FILTER_LIMIT = 200

const filterOptions = createFilterOptions<string>({
	limit: FILTER_LIMIT,
})

const OuterElementContext = createContext<HTMLAttributes<HTMLElement>>({})

const OuterElementType = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(
	function OuterElementType(props, ref) {
		const outerProps = useContext(OuterElementContext)

		return <div ref={ref} {...props} {...outerProps} />
	},
)

function renderRow({ data, index, style }: ListChildComponentProps<ReactElement[]>) {
	const option = data[index]

	return cloneElement(option, {
		style: {
			...option.props.style,
			...style,
			top: (style.top as number) + LISTBOX_PADDING,
		},
	})
}

const VirtualizedListbox = forwardRef<HTMLDivElement, HTMLAttributes<HTMLElement>>(
	function VirtualizedListbox(props, ref) {
		const { children, ...other } = props
		const itemData = Children.toArray(children) as ReactElement[]
		const itemCount = itemData.length
		const height = Math.min(itemCount, MAX_VISIBLE_OPTIONS) * OPTION_HEIGHT + LISTBOX_PADDING * 2

		return (
			<div ref={ref}>
				<OuterElementContext.Provider value={other}>
					<FixedSizeList
						height={height}
						width='100%'
						itemData={itemData}
						itemCount={itemCount}
						itemSize={OPTION_HEIGHT}
						innerElementType='ul'
						outerElementType={OuterElementType}
						overscanCount={5}
					>
						{renderRow}
					</FixedSizeList>
				</OuterElementContext.Provider>
			</div>
		)
	},
)

/**
 * Component that allows users to search and select a classification
 * from a list of available options. It features autocomplete capabilities
 * and displays truncated text in a tooltip when necessary.
 */
function SearchBar({ projectNames: classificationNames, setSelectedProject }: SearchBarProps) {
	return (
		<Stack className='bg-white' spacing={2}>
			<Autocomplete
				freeSolo
				id='free-solo-2-demo'
				disableClearable
				options={classificationNames}
				filterOptions={filterOptions}
				slots={{
					listbox: VirtualizedListbox,
				}}
				onChange={(_, newValue) => {
					setSelectedProject(newValue || '')
				}}
				onInputChange={(_, newInputValue) => {
					if (newInputValue === '') {
						setSelectedProject('')
					}
				}}
				renderInput={params => (
					<TextField
						{...params}
						label='Classification'
						InputProps={{
							...params.InputProps,
							type: 'search',
						}}
					/>
				)}
				renderOption={(props, option) => {
					const { key, ...otherProps } = props

					return (
						<Tooltip title={option} placement='right' arrow>
							<Box
								component='li'
								key={key}
								{...otherProps}
								sx={{
									display: 'flex',
									alignItems: 'center',
									minHeight: OPTION_HEIGHT,
									px: 2,
									overflow: 'hidden',
									textOverflow: 'ellipsis',
									whiteSpace: 'nowrap',
									transition: theme =>
										theme.transitions.create('background-color', {
											duration: theme.transitions.duration.shortest,
										}),
									'&:hover': {
										backgroundColor: 'action.hover',
									},
									'&.Mui-focused': {
										backgroundColor: 'action.hover',
									},
									'&[aria-selected="true"]': {
										backgroundColor: 'action.selected',
									},
									'&[aria-selected="true"].Mui-focused': {
										backgroundColor: theme =>
											`rgba(${theme.vars ? theme.vars.palette.primary.mainChannel : '25 118 210'} / ${theme.vars ? theme.vars.palette.action.selectedOpacity + theme.vars.palette.action.hoverOpacity : 0.2})`,
									},
								}}
							>
								{option}
							</Box>
						</Tooltip>
					)
				}}
			/>
		</Stack>
	)
}

export default memo(SearchBar)
