// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

export enum ClassificationRight {
	None = '#eeeeee',
	Read = '#c8e6b8',
	Write = '#b3e5fc',
	Mixed = '#dce775',
}

export function getRightLabel(value: string): string {
	return (Object.keys(ClassificationRight) as Array<keyof typeof ClassificationRight>).find(key => ClassificationRight[key] === value) ?? 'Unknown'
}
