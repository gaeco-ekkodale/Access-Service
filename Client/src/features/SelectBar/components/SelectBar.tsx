// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import Box from '@mui/material/Box'
import FormControl from '@mui/material/FormControl'
import InputLabel from '@mui/material/InputLabel'
import MenuItem from '@mui/material/MenuItem'
import Select, { SelectChangeEvent } from '@mui/material/Select'
import { useEffect } from 'react'

export type SelectBarProps<T> = {
	label: string
	idKey: keyof T
	nameKey: keyof T
	idBase: string
	hook: () => { isLoading: boolean; isError: boolean; data: T[] | undefined }
	onSelectedItemChange: (item: T) => void
	onQueryStateChange: (loading: boolean, error: boolean, data: T[] | undefined) => void
	selectedItemId?: string
	allOptionLabel?: string
	onAllSelected?: () => void
}

export default function SelectBar<T>({
	label,
	idKey,
	nameKey,
	idBase,
	hook,
	onSelectedItemChange,
	onQueryStateChange,
	selectedItemId,
	allOptionLabel,
	onAllSelected,
}: SelectBarProps<T>) {
	const { isLoading, isError, data } = hook()

	useEffect(() => {
		onQueryStateChange(isLoading, isError, data)
	}, [isLoading, isError, data, onQueryStateChange])

	const handleChange = (event: SelectChangeEvent) => {
		const nextId = event.target.value as string
		if (!nextId && allOptionLabel) {
			onAllSelected?.()
			return
		}
		const item = data?.find(i => String(i[idKey]) === nextId)
		if (item) {
			onSelectedItemChange(item)
		}
	}

	const currentValue = selectedItemId ?? ''
	const isUnavailable = isLoading || isError || !data || data.length === 0
	const fallbackOptionLabel = isLoading
		? `Loading ${label}...`
		: isError
			? `Failed to load ${label}`
			: `No ${label} available`

	const menuItems = !isUnavailable && data
		? [
			allOptionLabel
				? <MenuItem key='__all__' value=''><em>{allOptionLabel}</em></MenuItem>
				: null,
			...data.map(item => (
				<MenuItem key={String(item[idKey])} value={String(item[idKey])}>
					{String(item[nameKey])}
				</MenuItem>
			)),
		]
		: [<MenuItem key='__fallback__' disabled value=''>{fallbackOptionLabel}</MenuItem>]

	return (
		<div className='bg-white'>
			<Box>
				<FormControl fullWidth>
					<InputLabel id={`${idBase}-label`}>{label}</InputLabel>
					<Select
						labelId={`${idBase}-select-label`}
						id={`${idBase}-select`}
						value={currentValue}
						label={label}
						onChange={handleChange}
						autoWidth={false}
						disabled={isUnavailable}
					>
						{menuItems}
					</Select>
				</FormControl>
			</Box>
		</div>
	)
}
