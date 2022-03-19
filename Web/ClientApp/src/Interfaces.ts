export
	interface IMedium {
	id: number;
	name: string;
	ext: string;
	width: number;
	height: number;
	mediaType: string;
	size: number;
	hashKeyThumbNail: string;
		//calculated hash for this media
	hashKey: string;
	created: Date;
}
export interface IFile {
	file: number;
}
export interface IProgress {
	perc: number;
}
export
interface ICSRLine {
	/**e.g.00:00:00,000 --> 00:00:10,000 */
	timeLine: string;
	subtitles: string[]
}
export
interface ISubtitles {
	id: string;
	lines: string[];
}